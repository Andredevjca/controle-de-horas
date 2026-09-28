using ControleHoras.Models;

namespace ControleHoras.Interfaces.Services;

public interface IApontamentosServico
{
    Task LancarAsync(int usuarioId, LancamentoManual lancamento);
    Task<List<Demanda>> DemandasAsync(int usuarioId);
    Task<List<RegistroHistorico>> HistoricoAsync(int usuarioId);
    Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null);
}
