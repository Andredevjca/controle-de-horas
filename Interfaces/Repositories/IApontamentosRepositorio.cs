using ControleHoras.Models;

namespace ControleHoras.Interfaces.Repositories;

public interface IApontamentosRepositorio
{
    Task<List<Apontamento>> ApontamentosAsync(int usuarioId);
    Task<int> InserirApontamentoAsync(int usuarioId, int demandaId, DateTime inicio, DateTime? fim, string? descricao, bool manual);
    Task<int> FecharApontamentoAsync(int id, DateTime fim);
    Task<int> RegistrarPausaAsync(int apontamentoId, DateTime inicio);
    Task<int> FecharPausasAsync(int demandaId, DateTime fim);
}
