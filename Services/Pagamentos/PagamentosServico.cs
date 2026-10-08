using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using static ControleHoras.Services.Horario;
using static ControleHoras.Services.Validacao;

namespace ControleHoras.Services;

public class PagamentosServico(IPagamentosRepositorio pagamentos, IHistoricoRepositorio historico, IUnidadeTrabalho unidadeTrabalho, IPainelServico painel, IDemandasRepositorio demandas) : IPagamentosServico
{
    public Task PagarAsync(int usuarioId, Pagamento pagamento) => unidadeTrabalho.TransacionarAsync(usuarioId, async () => {
        Exigir(pagamento.DemandaId.HasValue && (await demandas.DemandasAsync(usuarioId)).Any(d => d.Id == pagamento.DemandaId), "Selecione uma demanda válida para o pagamento.");
        Exigir(pagamento.PeriodoInicio != default && pagamento.PeriodoFim >= pagamento.PeriodoInicio, "Informe uma competência válida.");
        var consulta = await painel.PainelAsync(usuarioId, new FiltroPeriodo { Inicio = pagamento.PeriodoInicio.Date, Fim = pagamento.PeriodoFim.Date });
        var pendente = consulta.DemandasPagamento.FirstOrDefault(d => d.Id == pagamento.DemandaId);
        Exigir(pendente != null && !pendente.CompetenciaSobreposta && (pendente.Saldo == null || pendente.Saldo > 0), "Esta demanda não possui saldo disponível na competência informada.");
        Exigir(pendente!.Saldo == null || pagamento.ValorPago <= pendente.Saldo, "O valor informado ultrapassa o saldo da demanda.");
        pagamento.DataPagamento = AgoraLocal.Date;
        Exigir(pagamento.ValorPago > 0, "Informe o valor pago.");
        await pagamentos.InserirPagamentoAsync(usuarioId, pagamento);
        await historico.AuditarAsync(usuarioId, pagamento.DemandaId, "Pagamento registrado", Formato.Dinheiro(pagamento.ValorPago));
    });

    public Task BaixarAsync(int usuarioId, BaixaPagamentos baixa) => unidadeTrabalho.TransacionarAsync(usuarioId, async () =>
    {
        Exigir(baixa.DemandaIds.Count > 0, "Selecione pelo menos uma demanda.");
        Exigir(baixa.Observacao == null || baixa.Observacao.Length <= 2000, "A observação deve ter até 2000 caracteres.");
        var atual = await PainelAsync(usuarioId, new FiltroPeriodo { Inicio = baixa.Inicio.Date, Fim = baixa.Fim.Date });
        var ids = baixa.DemandaIds.Distinct().ToList();
        var selecionadas = atual.DemandasPagamento.Where(d => ids.Contains(d.Id)).ToList();
        Exigir(selecionadas.Count == ids.Count && selecionadas.All(d => d.PodePagar), "A seleção contém demandas sem saldo disponível ou com recebimentos antigos sem demanda. Atualize o filtro e confira os saldos.");
        var data = AgoraLocal.Date;
        foreach (var demanda in selecionadas)
        {
            var pagamento = new Pagamento { DemandaId = demanda.Id, DataPagamento = data,
                PeriodoInicio = baixa.Inicio.Date, PeriodoFim = baixa.Fim.Date,
                ValorPago = demanda.Saldo!.Value, Observacao = baixa.Observacao };
            await pagamentos.InserirPagamentoAsync(usuarioId, pagamento);
            await historico.AuditarAsync(usuarioId, demanda.Id, "Pagamento registrado em lote", Formato.Dinheiro(pagamento.ValorPago));
        }
    });

    public async Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null)
    {
        var resultado = await painel.PainelAsync(usuarioId, filtro);
        SituacaoFinanceira.SomentePendentes(resultado);
        return resultado;
    }
}
