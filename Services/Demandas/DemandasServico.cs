using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using static ControleHoras.Services.Horario;
using static ControleHoras.Services.Validacao;
using static ControleHoras.Services.CalculoHoras;

namespace ControleHoras.Services;

public class DemandasServico(IDemandasRepositorio demandas, IApontamentosRepositorio apontamentosRepositorio, IHistoricoRepositorio historico, IUnidadeTrabalho unidadeTrabalho, IPainelServico painel) : IDemandasServico
{
    public async Task<DetalhesDemanda?> DetalhesDemandaAsync(int usuarioId, int demandaId)
    {
        var demanda = (await demandas.DemandasAsync(usuarioId)).FirstOrDefault(d => d.Id == demandaId);
        if (demanda == null) return null;
        var apontamentos = (await apontamentosRepositorio.ApontamentosAsync(usuarioId)).Where(a => a.DemandaId == demandaId).ToList();
        var detalhes = new DetalhesDemanda { Demanda = demanda };
        if (apontamentos.Count > 0)
            detalhes.Dias = Recortar(apontamentos, Local(apontamentos.Min(a => a.Inicio)).Date, AgoraLocal.Date)
                .GroupBy(l => l.Data)
                .Select(g => new HorasDia { Data = g.Key, Segundos = g.Sum(l => l.Segundos) })
                .OrderByDescending(d => d.Data).ToList();
        demanda.Segundos = (long)detalhes.SegundosTotais;
        return detalhes;
    }

    public Task SalvarDemandaAsync(int usuarioId, Demanda demanda) => unidadeTrabalho.TransacionarAsync(usuarioId, async () => {
        Exigir(demanda.DataRecebimento != default, "Informe a data de recebimento.");
        Exigir(!demanda.Prazo.HasValue || demanda.Prazo.Value.Date >= demanda.DataRecebimento.Date, "O prazo deve ser igual ou posterior ao recebimento.");
        Exigir(!demanda.ProjetoId.HasValue || (await demandas.ProjetosAsync(usuarioId)).Any(p => p.Id == demanda.ProjetoId), "Projeto inválido.");
        if (demanda.Id > 0) Exigir((await demandas.DemandasAsync(usuarioId)).Any(d => d.Id == demanda.Id), "Demanda não encontrada.");
        demanda.UsuarioId = usuarioId;
        demanda.Status = "Pendente";
        var id = await demandas.SalvarDemandaAsync(demanda);
        await historico.AuditarAsync(usuarioId, id, demanda.Id == 0 ? "Demanda criada" : "Demanda editada", demanda.Titulo);
    });

    public Task MudarTrabalhoAsync(int usuarioId, int demandaId, string acao) => unidadeTrabalho.TransacionarAsync(usuarioId, async () => {
        var demanda = (await demandas.DemandasAsync(usuarioId)).FirstOrDefault(d => d.Id == demandaId);
        Exigir(demanda != null, "Demanda não encontrada.");
        var registros = await apontamentosRepositorio.ApontamentosAsync(usuarioId);
        var ativo = registros.FirstOrDefault(a => a.Fim == null);
        var agora = DateTime.UtcNow;
        string status;
        if (acao == "iniciar")
        {
            Exigir(ativo == null, "Pause o trabalho em andamento antes de iniciar outro.");
            Exigir(demanda!.Status is "Pendente" or "Pausada", "Somente demandas pendentes ou pausadas podem ser iniciadas.");
            await apontamentosRepositorio.FecharPausasAsync(demandaId, agora);
            await apontamentosRepositorio.InserirApontamentoAsync(usuarioId, demandaId, agora, null, null, false);
            status = "Em andamento";
        }
        else if (acao is "pausar" or "finalizar" or "cancelar" or "reabrir")
        {
            Exigir(acao != "pausar" || ativo?.DemandaId == demandaId, "Esta demanda não está em execução.");
            Exigir(acao != "reabrir" || demanda!.Status is "Concluída" or "Cancelada", "Somente demandas encerradas podem ser reabertas.");
            Exigir(acao == "reabrir" || demanda!.Status is not ("Concluída" or "Cancelada"), "A demanda já está encerrada.");
            if (ativo?.DemandaId == demandaId)
            {
                await apontamentosRepositorio.FecharApontamentoAsync(ativo.Id, agora);
                if (acao == "pausar") await apontamentosRepositorio.RegistrarPausaAsync(ativo.Id, agora);
            }
            if (acao != "pausar") await apontamentosRepositorio.FecharPausasAsync(demandaId, agora);
            status = acao switch { "pausar" => "Pausada", "finalizar" => "Concluída", "cancelar" => "Cancelada", _ => "Pendente" };
        }
        else throw new InvalidOperationException("Ação inválida.");
        await demandas.AtualizarStatusAsync(usuarioId, demandaId, status, status is "Concluída" or "Cancelada" ? agora : null);
        await historico.AuditarAsync(usuarioId, demandaId, $"Status: {status}");
    });
    public Task<List<Demanda>> DemandasAsync(int usuarioId) => demandas.DemandasAsync(usuarioId);
    public Task<List<Projeto>> ProjetosAsync(int usuarioId) => demandas.ProjetosAsync(usuarioId);
    public Task<List<RegistroHistorico>> HistoricoAsync(int usuarioId) => historico.HistoricoAsync(usuarioId);
    public Task CriarProjetoAsync(int usuarioId, string nome)
    {
        Exigir(!string.IsNullOrWhiteSpace(nome) && nome.Trim().Length <= 120, "Informe um nome de projeto de até 120 caracteres.");
        return demandas.CriarProjetoAsync(usuarioId, nome.Trim());
    }
    public Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null) => painel.PainelAsync(usuarioId, filtro);
}
