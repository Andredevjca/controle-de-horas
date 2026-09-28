using ControleHoras.Models;

namespace ControleHoras.Interfaces.Services;

public interface IDemandasServico
{
    Task<DetalhesDemanda?> DetalhesDemandaAsync(int usuarioId, int demandaId);
    Task SalvarDemandaAsync(int usuarioId, Demanda demanda);
    Task MudarTrabalhoAsync(int usuarioId, int demandaId, string acao);
    Task<List<Demanda>> DemandasAsync(int usuarioId);
    Task<List<Projeto>> ProjetosAsync(int usuarioId);
    Task<List<RegistroHistorico>> HistoricoAsync(int usuarioId);
    Task CriarProjetoAsync(int usuarioId, string nome);
    Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null);
}
