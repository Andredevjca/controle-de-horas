using ControleHoras.Models;
namespace ControleHoras.Services;

public static class SituacaoFinanceira
{
    // Calculado antes dos filtros: uma consulta menor não volta a apresentar horas quitadas.
    // O recebimento é distribuído proporcionalmente ao saldo das horas da sua competência.
    public static void Aplicar(List<LinhaRelatorio> linhas, List<Pagamento> pagamentos, decimal? tarifa)
    {
        foreach (var grupo in linhas.GroupBy(l => l.DemandaId))
        {
            decimal acumulado = 0, arredondado = 0;
            foreach (var linha in grupo.OrderBy(l => l.Inicio).ThenBy(l => l.Fim))
            {
                linha.Pago = 0;
                if (!tarifa.HasValue) { linha.Estimado = null; continue; }
                acumulado += linha.Valor(tarifa)!.Value;
                var total = Math.Round(acumulado, 2, MidpointRounding.AwayFromZero);
                linha.Estimado = total - arredondado;
                arredondado = total;
            }
        }
        foreach (var pagamento in pagamentos.Where(p => p.DemandaId.HasValue).OrderBy(p => p.DataPagamento).ThenBy(p => p.Id))
        {
            if (!tarifa.HasValue)
            {
                var semTarifa = linhas.Where(l => l.DemandaId == pagamento.DemandaId && l.Data >= pagamento.PeriodoInicio.Date && l.Data <= pagamento.PeriodoFim.Date).OrderBy(l => l.Inicio).ToList();
                var segundos = semTarifa.Sum(l => l.Segundos);
                var valor = pagamento.ValorPago;
                foreach (var linha in semTarifa)
                {
                    var parte = segundos > 0 ? Math.Round(valor * (decimal)(linha.Segundos / segundos), 2, MidpointRounding.AwayFromZero) : 0;
                    linha.Pago += parte;
                    valor -= parte;
                    segundos -= linha.Segundos;
                }
                continue;
            }
            var elegiveis = linhas.Where(l => l.DemandaId == pagamento.DemandaId && l.Data >= pagamento.PeriodoInicio.Date && l.Data <= pagamento.PeriodoFim.Date && l.Saldo > 0)
                .OrderBy(l => l.Inicio).ThenBy(l => l.Fim).ToList();
            var saldo = elegiveis.Sum(l => l.Saldo!.Value);
            var restante = Math.Min(pagamento.ValorPago, saldo);
            foreach (var linha in elegiveis)
            {
                var parte = saldo == 0 ? 0 : Math.Round(restante * linha.Saldo!.Value / saldo, 2, MidpointRounding.AwayFromZero);
                parte = Math.Min(restante, Math.Min(linha.Saldo!.Value, parte));
                saldo -= linha.Saldo!.Value;
                linha.Pago += parte;
                restante -= parte;
            }
        }
    }

    public static List<DemandaPagamento> Resumir(Painel painel) => painel.Linhas.GroupBy(l => l.DemandaId).Select(g => new DemandaPagamento {
        Id = g.Key, Titulo = g.First().Titulo, Segundos = g.Sum(l => l.Segundos),
        Estimado = painel.ValorHora.HasValue ? g.Sum(l => l.Estimado ?? 0) : null, Pago = g.Sum(l => l.Pago),
        CompetenciaSobreposta = painel.Pagamentos.Any(p => p.DemandaId == null)
    }).OrderBy(d => d.Titulo).ToList();

    public static void SomentePendentes(Painel painel)
    {
        painel.Demandas = painel.Demandas.Where(d => !d.Quitada).ToList();
        painel.Linhas = painel.Linhas.Where(l => l.SituacaoPagamento != "Pago").ToList();
        painel.DemandasPagamento = Resumir(painel).Where(d => d.Saldo == null || d.Saldo > 0).ToList();
    }
}
