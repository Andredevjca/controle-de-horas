using ControleHoras.Models;

namespace ControleHoras.Interfaces.Services;

public interface IRelatoriosServico
{
    Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null);
    Task<byte[]> ExportarExcelAsync(int usuarioId, FiltroPeriodo filtro, string? nomeUsuario);
}
