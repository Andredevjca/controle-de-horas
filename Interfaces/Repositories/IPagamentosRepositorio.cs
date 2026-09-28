using ControleHoras.Models;

namespace ControleHoras.Interfaces.Repositories;

public interface IPagamentosRepositorio
{
    Task<List<Pagamento>> PagamentosAsync(int usuarioId);
    Task<int> InserirPagamentoAsync(int usuarioId, Pagamento pagamento);
}
