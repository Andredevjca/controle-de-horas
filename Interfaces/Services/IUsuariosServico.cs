using ControleHoras.Models;

namespace ControleHoras.Interfaces.Services;

public interface IUsuariosServico
{
    Task SalvarUsuarioAsync(int operadorId, EdicaoUsuario usuario);
    Task<List<Usuario>> UsuariosAsync();
    Task<Usuario?> ObterUsuarioAsync(int id);
}
