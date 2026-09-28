using ControleHoras.Models;

namespace ControleHoras.Interfaces.Services;

public interface IDashboardServico
{
    Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null);
}
