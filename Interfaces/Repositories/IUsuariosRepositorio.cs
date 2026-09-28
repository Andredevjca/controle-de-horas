using ControleHoras.Models;

namespace ControleHoras.Interfaces.Repositories;

public interface IUsuariosRepositorio
{
    Task<Usuario?> ObterUsuarioAsync(int id);
    Task<Usuario?> BuscarLoginAsync(string login);
    Task<List<Usuario>> UsuariosAsync();
    Task SalvarUsuarioAsync(EdicaoUsuario usuario, string? senha);
}
