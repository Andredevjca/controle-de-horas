using ControleHoras.Models;

namespace ControleHoras.Interfaces.Services;

public interface IPagamentosServico
{
    Task PagarAsync(int usuarioId, Pagamento pagamento);
    Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null);
}
