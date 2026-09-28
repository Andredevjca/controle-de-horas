using ControleHoras.Models;

namespace ControleHoras.Interfaces.Repositories;

public interface IHistoricoRepositorio
{
    Task<int> AuditarAsync(int usuarioId, int? demandaId, string acao, string? detalhes = null);
    Task<List<RegistroHistorico>> HistoricoAsync(int usuarioId);
}
