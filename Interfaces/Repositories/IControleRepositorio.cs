using ControleHoras.Models;
namespace ControleHoras.Interfaces.Repositories;
public interface IControleRepositorio
{
    Task TransacionarAsync(int usuarioId, Func<Task> operacao);
    Task<Usuario?> ObterUsuarioAsync(int id);
    Task<Usuario?> BuscarLoginAsync(string login);
    Task<List<Usuario>> UsuariosAsync();
    Task SalvarUsuarioAsync(EdicaoUsuario usuario, string? senha);
    Task<List<Demanda>> DemandasAsync(int usuarioId);
    Task<List<Projeto>> ProjetosAsync(int usuarioId);
    Task<int> CriarProjetoAsync(int usuarioId, string nome);
    Task<int> SalvarDemandaAsync(Demanda demanda);
    Task<int> AtualizarStatusAsync(int usuarioId, int id, string status, DateTime? finalizacao);
    Task<List<Apontamento>> ApontamentosAsync(int usuarioId);
    Task<int> InserirApontamentoAsync(int usuarioId, int demandaId, DateTime inicio, DateTime? fim, string? descricao, bool manual);
    Task<int> FecharApontamentoAsync(int id, DateTime fim);
    Task<int> RegistrarPausaAsync(int apontamentoId, DateTime inicio);
    Task<int> FecharPausasAsync(int demandaId, DateTime fim);
    Task<decimal?> ValorHoraAsync(int usuarioId);
    Task<int> DefinirValorAsync(int usuarioId, decimal? valor);
    Task<List<Pagamento>> PagamentosAsync(int usuarioId);
    Task<int> InserirPagamentoAsync(int usuarioId, Pagamento pagamento);
    Task<int> AuditarAsync(int usuarioId, int? demandaId, string acao, string? detalhes = null);
    Task<List<RegistroHistorico>> HistoricoAsync(int usuarioId);
}
