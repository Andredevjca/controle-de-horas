using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace ControleHoras.Services;
public class ControleServico(IControleRepositorio repositorio, IPasswordHasher<Usuario> senhas) : IControleServico
{
    public static readonly TimeZoneInfo Fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    public static DateTime AgoraLocal => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Fuso);
    public static DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Fuso);
    public static DateTime Utc(DateTime local) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Fuso);
    public static void Exigir(bool condicao, string mensagem) { if (!condicao) throw new InvalidOperationException(mensagem); }
    public async Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null)
    {
        filtro ??= new() { Inicio = new(AgoraLocal.Year, AgoraLocal.Month, 1), Fim = AgoraLocal.Date };
        Exigir(filtro.Inicio != default && filtro.Fim >= filtro.Inicio && (filtro.Fim - filtro.Inicio).TotalDays <= 3660, "Informe um período válido de até 10 anos.");
        var apontamentos = await repositorio.ApontamentosAsync(usuarioId);
        var pagamentos = await repositorio.PagamentosAsync(usuarioId);
        var painel = new Painel { Filtro = filtro, Demandas = await repositorio.DemandasAsync(usuarioId), Projetos = await repositorio.ProjetosAsync(usuarioId), ValorHora = await repositorio.ValorHoraAsync(usuarioId) };
        painel.Linhas = Recortar(apontamentos.Where(a => (!filtro.DemandaId.HasValue || a.DemandaId == filtro.DemandaId) && (string.IsNullOrEmpty(filtro.Status) || a.Status == filtro.Status)), filtro.Inicio, filtro.Fim);
        // Pagamentos são atribuídos integralmente à competência informada, nunca rateados silenciosamente.
        painel.Pagamentos = pagamentos.Where(p => p.PeriodoInicio.Date >= filtro.Inicio.Date && p.PeriodoFim.Date <= filtro.Fim.Date).ToList();
        painel.SegundosHoje = Recortar(apontamentos, AgoraLocal.Date, AgoraLocal.Date).Sum(l => l.Segundos);
        painel.SegundosMes = Recortar(apontamentos, new(AgoraLocal.Year, AgoraLocal.Month, 1), AgoraLocal.Date).Sum(l => l.Segundos);
        painel.SegundosTotais = apontamentos.Sum(a => ((a.Fim ?? DateTime.UtcNow) - a.Inicio).TotalSeconds);
        painel.SaldoGeral = painel.ValorHora.HasValue ? (decimal)painel.SegundosTotais / 3600m * painel.ValorHora.Value - pagamentos.Sum(p => p.ValorPago) : null;
        return painel;
    }
    public async Task<DetalhesDemanda?> DetalhesDemandaAsync(int usuarioId, int demandaId)
    {
        var demanda = (await repositorio.DemandasAsync(usuarioId)).FirstOrDefault(d => d.Id == demandaId);
        if (demanda == null) return null;
        var apontamentos = (await repositorio.ApontamentosAsync(usuarioId)).Where(a => a.DemandaId == demandaId).ToList();
        var detalhes = new DetalhesDemanda { Demanda = demanda };
        if (apontamentos.Count > 0)
            detalhes.Dias = Recortar(apontamentos, Local(apontamentos.Min(a => a.Inicio)).Date, AgoraLocal.Date)
                .GroupBy(l => l.Data)
                .Select(g => new HorasDia { Data = g.Key, Segundos = g.Sum(l => l.Segundos) })
                .OrderByDescending(d => d.Data).ToList();
        demanda.Segundos = (long)detalhes.SegundosTotais;
        return detalhes;
    }
    public static List<LinhaRelatorio> Recortar(IEnumerable<Apontamento> apontamentos, DateTime inicio, DateTime fim)
    {
        var linhas = new List<LinhaRelatorio>();
        var agora = DateTime.UtcNow;
        foreach (var apontamento in apontamentos)
        {
            var primeiro = Local(apontamento.Inicio);
            var ultimo = Local(apontamento.Fim ?? agora);
            var cursor = primeiro > inicio.Date ? primeiro : inicio.Date;
            var limite = ultimo < fim.Date.AddDays(1) ? ultimo : fim.Date.AddDays(1);
            while (cursor < limite)
            {
                var ate = cursor.Date.AddDays(1) < limite ? cursor.Date.AddDays(1) : limite;
                linhas.Add(new() { Data = cursor.Date, DemandaId = apontamento.DemandaId, Titulo = apontamento.Titulo, Inicio = cursor, Fim = ate, EmAndamento = apontamento.Fim == null, Descricao = apontamento.Descricao });
                cursor = ate;
            }
        }
        return linhas.OrderByDescending(l => l.Inicio).ToList();
    }
    public Task SalvarDemandaAsync(int usuarioId, Demanda demanda) => repositorio.TransacionarAsync(usuarioId, async () => {
        Exigir(demanda.DataRecebimento != default, "Informe a data de recebimento.");
        Exigir(!demanda.Prazo.HasValue || demanda.Prazo.Value.Date >= demanda.DataRecebimento.Date, "O prazo deve ser igual ou posterior ao recebimento.");
        Exigir(!demanda.ProjetoId.HasValue || (await repositorio.ProjetosAsync(usuarioId)).Any(p => p.Id == demanda.ProjetoId), "Projeto inválido.");
        if (demanda.Id > 0) Exigir((await repositorio.DemandasAsync(usuarioId)).Any(d => d.Id == demanda.Id), "Demanda não encontrada.");
        demanda.UsuarioId = usuarioId;
        demanda.Status = "Pendente";
        var id = await repositorio.SalvarDemandaAsync(demanda);
        await repositorio.AuditarAsync(usuarioId, id, demanda.Id == 0 ? "Demanda criada" : "Demanda editada", demanda.Titulo);
    });
    public Task MudarTrabalhoAsync(int usuarioId, int demandaId, string acao) => repositorio.TransacionarAsync(usuarioId, async () => {
        var demanda = (await repositorio.DemandasAsync(usuarioId)).FirstOrDefault(d => d.Id == demandaId);
        Exigir(demanda != null, "Demanda não encontrada.");
        var registros = await repositorio.ApontamentosAsync(usuarioId);
        var ativo = registros.FirstOrDefault(a => a.Fim == null);
        var agora = DateTime.UtcNow;
        string status;
        if (acao == "iniciar")
        {
            Exigir(ativo == null, "Pause o trabalho em andamento antes de iniciar outro.");
            Exigir(demanda!.Status is "Pendente" or "Pausada", "Somente demandas pendentes ou pausadas podem ser iniciadas.");
            await repositorio.FecharPausasAsync(demandaId, agora);
            await repositorio.InserirApontamentoAsync(usuarioId, demandaId, agora, null, null, false);
            status = "Em andamento";
        }
        else if (acao is "pausar" or "finalizar" or "cancelar" or "reabrir")
        {
            Exigir(acao != "pausar" || ativo?.DemandaId == demandaId, "Esta demanda não está em execução.");
            Exigir(acao != "reabrir" || demanda!.Status is "Concluída" or "Cancelada", "Somente demandas encerradas podem ser reabertas.");
            Exigir(acao == "reabrir" || demanda!.Status is not ("Concluída" or "Cancelada"), "A demanda já está encerrada.");
            if (ativo?.DemandaId == demandaId)
            {
                await repositorio.FecharApontamentoAsync(ativo.Id, agora);
                if (acao == "pausar") await repositorio.RegistrarPausaAsync(ativo.Id, agora);
            }
            if (acao != "pausar") await repositorio.FecharPausasAsync(demandaId, agora);
            status = acao switch { "pausar" => "Pausada", "finalizar" => "Concluída", "cancelar" => "Cancelada", _ => "Pendente" };
        }
        else throw new InvalidOperationException("Ação inválida.");
        await repositorio.AtualizarStatusAsync(usuarioId, demandaId, status, status is "Concluída" or "Cancelada" ? agora : null);
        await repositorio.AuditarAsync(usuarioId, demandaId, $"Status: {status}");
    });
    public Task LancarAsync(int usuarioId, LancamentoManual lancamento) => repositorio.TransacionarAsync(usuarioId, async () => {
        Exigir((await repositorio.DemandasAsync(usuarioId)).Any(d => d.Id == lancamento.DemandaId), "Selecione uma demanda válida.");
        Exigir(lancamento.Data != default && lancamento.Inicio >= TimeSpan.Zero && lancamento.Inicio < TimeSpan.FromDays(1) && lancamento.Fim >= TimeSpan.Zero && lancamento.Fim < TimeSpan.FromDays(1), "Informe data e horários válidos.");
        Exigir(lancamento.Inicio != lancamento.Fim, "Início e fim devem ser diferentes.");
        var inicio = Utc(lancamento.Data.Date + lancamento.Inicio);
        var fim = Utc(lancamento.Data.Date.AddDays(lancamento.Fim < lancamento.Inicio ? 1 : 0) + lancamento.Fim);
        Exigir(fim <= DateTime.UtcNow, "Não é possível registrar horas futuras.");
        Exigir(!(await repositorio.ApontamentosAsync(usuarioId)).Any(a => inicio < (a.Fim ?? DateTime.MaxValue) && fim > a.Inicio), "O período se sobrepõe a outro apontamento.");
        await repositorio.InserirApontamentoAsync(usuarioId, lancamento.DemandaId, inicio, fim, lancamento.Descricao, true);
        await repositorio.AuditarAsync(usuarioId, lancamento.DemandaId, "Apontamento manual", $"{lancamento.Data:dd/MM/yyyy} {lancamento.Inicio} até {lancamento.Fim}");
    });
    public Task DefinirValorAsync(int usuarioId, decimal? valor) => repositorio.TransacionarAsync(usuarioId, async () => {
        Exigir(valor == null || valor > 0 && valor <= 99999999.99m, "Informe um valor positivo ou deixe em branco.");
        var anterior = await repositorio.ValorHoraAsync(usuarioId);
        await repositorio.DefinirValorAsync(usuarioId, valor);
        await repositorio.AuditarAsync(usuarioId, null, "Valor/hora alterado", $"{Formato.Dinheiro(anterior)} → {Formato.Dinheiro(valor)}");
    });
    public Task PagarAsync(int usuarioId, Pagamento pagamento) => repositorio.TransacionarAsync(usuarioId, async () => {
        Exigir(pagamento.PeriodoInicio != default && pagamento.PeriodoFim >= pagamento.PeriodoInicio, "Informe uma competência válida.");
        Exigir(pagamento.DataPagamento != default && pagamento.DataPagamento.Date <= AgoraLocal.Date, "Informe uma data de pagamento válida, não futura.");
        Exigir(pagamento.ValorPago > 0, "Informe o valor pago.");
        await repositorio.InserirPagamentoAsync(usuarioId, pagamento);
        await repositorio.AuditarAsync(usuarioId, null, "Pagamento registrado", Formato.Dinheiro(pagamento.ValorPago));
    });
    public Task SalvarUsuarioAsync(int operadorId, EdicaoUsuario usuario) => repositorio.TransacionarAsync(operadorId, async () => {
        var existente = usuario.Id == 0 ? null : await repositorio.ObterUsuarioAsync(usuario.Id);
        Exigir(usuario.Id == 0 || existente != null, "Usuário não encontrado.");
        Exigir(usuario.Id != operadorId || usuario.Ativo, "Você não pode inativar seu próprio usuário.");
        Exigir(existente?.Administrador != true || usuario.Ativo, "O administrador inicial deve permanecer ativo.");
        Exigir(usuario.Id != 0 || !string.IsNullOrWhiteSpace(usuario.NovaSenha), "Informe uma senha para o novo usuário.");
        Exigir(usuario.Ativo || !(await repositorio.ApontamentosAsync(usuario.Id)).Any(a => a.Fim == null), "Pause o cronômetro deste usuário antes de inativá-lo.");
        var duplicado = await repositorio.BuscarLoginAsync(usuario.Login);
        Exigir(duplicado == null || duplicado.Id == usuario.Id, "Este nome de usuário já existe.");
        await repositorio.SalvarUsuarioAsync(usuario, string.IsNullOrEmpty(usuario.NovaSenha) ? null : senhas.HashPassword(usuario, usuario.NovaSenha));
        await repositorio.AuditarAsync(operadorId, null, "Usuário salvo", usuario.Login);
    });
}

