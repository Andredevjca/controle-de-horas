using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace ControleHoras.Services;

public class AutenticacaoServico(IUsuariosRepositorio usuarios, IPasswordHasher<Usuario> senhas) : IAutenticacaoServico
{
    public async Task<Usuario?> AutenticarAsync(string? login, string? senha)
    {
        var usuario = string.IsNullOrWhiteSpace(login) ? null : await usuarios.BuscarLoginAsync(login);
        return usuario == null || !usuario.Ativo || string.IsNullOrEmpty(senha) || senhas.VerifyHashedPassword(usuario, usuario.Senha, senha) == PasswordVerificationResult.Failed ? null : usuario;
    }
    public Task<Usuario?> ObterUsuarioAsync(int id) => usuarios.ObterUsuarioAsync(id);
}
