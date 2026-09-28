using ControleHoras.Models;

namespace ControleHoras.Interfaces.Repositories;

public interface IDemandasRepositorio
{
    Task<List<Demanda>> DemandasAsync(int usuarioId);
    Task<List<Projeto>> ProjetosAsync(int usuarioId);
    Task<int> CriarProjetoAsync(int usuarioId, string nome);
    Task<int> SalvarDemandaAsync(Demanda demanda);
    Task<int> AtualizarStatusAsync(int usuarioId, int id, string status, DateTime? finalizacao);
}
