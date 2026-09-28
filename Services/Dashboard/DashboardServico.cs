using ControleHoras.Models;
using ControleHoras.Interfaces.Services;

namespace ControleHoras.Services;

public class DashboardServico(IPainelServico painel) : IDashboardServico
{
    public Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null) => painel.PainelAsync(usuarioId, filtro);
}
