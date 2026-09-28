using ControleHoras.Models;
namespace ControleHoras.Interfaces.Services;
public interface IControleServico
{
    Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null);
    Task<DetalhesDemanda?> DetalhesDemandaAsync(int usuarioId, int demandaId);
    Task SalvarDemandaAsync(int usuarioId, Demanda demanda);
    Task MudarTrabalhoAsync(int usuarioId, int demandaId, string acao);
    Task LancarAsync(int usuarioId, LancamentoManual lancamento);
    Task DefinirValorAsync(int usuarioId, decimal? valor);
    Task PagarAsync(int usuarioId, Pagamento pagamento);
    Task SalvarUsuarioAsync(int operadorId, EdicaoUsuario usuario);
}
