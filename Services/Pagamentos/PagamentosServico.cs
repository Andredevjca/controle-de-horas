using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using static ControleHoras.Services.Horario;
using static ControleHoras.Services.Validacao;

namespace ControleHoras.Services;

public class PagamentosServico(IPagamentosRepositorio pagamentos, IHistoricoRepositorio historico, IUnidadeTrabalho unidadeTrabalho, IPainelServico painel) : IPagamentosServico
{
    public Task PagarAsync(int usuarioId, Pagamento pagamento) => unidadeTrabalho.TransacionarAsync(usuarioId, async () => {
        Exigir(pagamento.PeriodoInicio != default && pagamento.PeriodoFim >= pagamento.PeriodoInicio, "Informe uma competência válida.");
        Exigir(pagamento.DataPagamento != default && pagamento.DataPagamento.Date <= AgoraLocal.Date, "Informe uma data de pagamento válida, não futura.");
        Exigir(pagamento.ValorPago > 0, "Informe o valor pago.");
        await pagamentos.InserirPagamentoAsync(usuarioId, pagamento);
        await historico.AuditarAsync(usuarioId, null, "Pagamento registrado", Formato.Dinheiro(pagamento.ValorPago));
    });

    public Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null) => painel.PainelAsync(usuarioId, filtro);
}
