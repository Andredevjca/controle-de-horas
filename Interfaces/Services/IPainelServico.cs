using ControleHoras.Models;

namespace ControleHoras.Interfaces.Services;

public interface IPainelServico
{
    Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null);
}
