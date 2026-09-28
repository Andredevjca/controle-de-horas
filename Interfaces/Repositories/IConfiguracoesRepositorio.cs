using ControleHoras.Models;

namespace ControleHoras.Interfaces.Repositories;

public interface IConfiguracoesRepositorio
{
    Task<decimal?> ValorHoraAsync(int usuarioId);
    Task<int> DefinirValorAsync(int usuarioId, decimal? valor);
}
