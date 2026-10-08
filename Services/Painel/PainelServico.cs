using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using static ControleHoras.Services.Horario;
using static ControleHoras.Services.Validacao;
using static ControleHoras.Services.CalculoHoras;

namespace ControleHoras.Services;

public class PainelServico(IDemandasRepositorio demandas, IApontamentosRepositorio apontamentosRepositorio, IConfiguracoesRepositorio configuracoes, IPagamentosRepositorio pagamentosRepositorio) : IPainelServico
{
    public async Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null)
    {
        filtro ??= new() { Inicio = new(AgoraLocal.Year, AgoraLocal.Month, 1), Fim = AgoraLocal.Date };
        Exigir(filtro.Inicio != default && filtro.Fim >= filtro.Inicio && (filtro.Fim - filtro.Inicio).TotalDays <= 3660, "Informe um período válido de até 10 anos.");
        var apontamentos = await apontamentosRepositorio.ApontamentosAsync(usuarioId);
        var pagamentos = await pagamentosRepositorio.PagamentosAsync(usuarioId);
        var painel = new Painel { Filtro = filtro, Demandas = await demandas.DemandasAsync(usuarioId), Projetos = await demandas.ProjetosAsync(usuarioId), ValorHora = await configuracoes.ValorHoraAsync(usuarioId) };
        var todasLinhas = apontamentos.Count == 0 ? new List<LinhaRelatorio>() : Recortar(apontamentos, Local(apontamentos.Min(a => a.Inicio)).Date, AgoraLocal.Date);
        SituacaoFinanceira.Aplicar(todasLinhas, pagamentos, painel.ValorHora);
        foreach (var demanda in painel.Demandas)
        {
            var horas = todasLinhas.Where(l => l.DemandaId == demanda.Id).ToList();
            var recebeu = pagamentos.Any(p => p.DemandaId == demanda.Id && p.ValorPago > 0);
            demanda.SituacaoPagamento = recebeu && painel.ValorHora.HasValue && horas.Count > 0 && horas.Sum(l => l.Saldo ?? 0) == 0 ? "Pago" : recebeu ? "Parcial" : "Não pago";
        }
        painel.Linhas = todasLinhas.Where(l => l.Data >= filtro.Inicio.Date && l.Data <= filtro.Fim.Date && (!filtro.DemandaId.HasValue || l.DemandaId == filtro.DemandaId) && (string.IsNullOrEmpty(filtro.Status) || painel.Demandas.Any(d => d.Id == l.DemandaId && d.Status == filtro.Status)) && (string.IsNullOrEmpty(filtro.Pagamento) || (filtro.Pagamento == "Pendente" ? l.SituacaoPagamento != "Pago" : l.SituacaoPagamento == filtro.Pagamento))).ToList();
        // Os recibos mantêm seus valores originais; os indicadores usam a parcela atribuída às horas filtradas.
        painel.Pagamentos = pagamentos.Where(p => p.PeriodoInicio.Date <= filtro.Fim.Date && p.PeriodoFim.Date >= filtro.Inicio.Date && (!filtro.DemandaId.HasValue || p.DemandaId == filtro.DemandaId) && (string.IsNullOrEmpty(filtro.Status) || painel.Demandas.Any(d => d.Id == p.DemandaId && d.Status == filtro.Status)) && (string.IsNullOrEmpty(filtro.Pagamento) || painel.Linhas.Any(l => l.DemandaId == p.DemandaId))).ToList();
        painel.SegundosHoje = Recortar(apontamentos, AgoraLocal.Date, AgoraLocal.Date).Sum(l => l.Segundos);
        painel.SegundosMes = Recortar(apontamentos, new(AgoraLocal.Year, AgoraLocal.Month, 1), AgoraLocal.Date).Sum(l => l.Segundos);
        painel.SegundosTotais = apontamentos.Sum(a => ((a.Fim ?? DateTime.UtcNow) - a.Inicio).TotalSeconds);
        painel.SaldoGeral = painel.ValorHora.HasValue ? (decimal)painel.SegundosTotais / 3600m * painel.ValorHora.Value - pagamentos.Sum(p => p.ValorPago) : null;
        painel.DemandasPagamento = SituacaoFinanceira.Resumir(painel);
        return painel;
    }
}
