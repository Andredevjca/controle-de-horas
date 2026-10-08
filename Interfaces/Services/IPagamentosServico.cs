using ControleHoras.Models;

namespace ControleHoras.Interfaces.Services;

public interface IPagamentosServico
{
    Task BaixarAsync(int usuarioId, BaixaPagamentos baixa);
    Task PagarAsync(int usuarioId, Pagamento pagamento);
    Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null);
}
