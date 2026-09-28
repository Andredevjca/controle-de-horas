using ControleHoras.Models;

namespace ControleHoras.Interfaces.Services;

public interface IAutenticacaoServico
{
    Task<Usuario?> AutenticarAsync(string? login, string? senha);
    Task<Usuario?> ObterUsuarioAsync(int id);
}
