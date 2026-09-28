using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using static ControleHoras.Services.Validacao;

namespace ControleHoras.Services;

public class UsuariosServico(IUsuariosRepositorio usuarios, IApontamentosRepositorio apontamentosRepositorio, IHistoricoRepositorio historico, IUnidadeTrabalho unidadeTrabalho, IPasswordHasher<Usuario> senhas) : IUsuariosServico
{
    public Task SalvarUsuarioAsync(int operadorId, EdicaoUsuario usuario) => unidadeTrabalho.TransacionarAsync(operadorId, async () => {
        var existente = usuario.Id == 0 ? null : await usuarios.ObterUsuarioAsync(usuario.Id);
        Exigir(usuario.Id == 0 || existente != null, "Usuário não encontrado.");
        Exigir(usuario.Id != operadorId || usuario.Ativo, "Você não pode inativar seu próprio usuário.");
        Exigir(existente?.Administrador != true || usuario.Ativo, "O administrador inicial deve permanecer ativo.");
        Exigir(usuario.Id != 0 || !string.IsNullOrWhiteSpace(usuario.NovaSenha), "Informe uma senha para o novo usuário.");
        Exigir(usuario.Ativo || !(await apontamentosRepositorio.ApontamentosAsync(usuario.Id)).Any(a => a.Fim == null), "Pause o cronômetro deste usuário antes de inativá-lo.");
        var duplicado = await usuarios.BuscarLoginAsync(usuario.Login);
        Exigir(duplicado == null || duplicado.Id == usuario.Id, "Este nome de usuário já existe.");
        await usuarios.SalvarUsuarioAsync(usuario, string.IsNullOrEmpty(usuario.NovaSenha) ? null : senhas.HashPassword(usuario, usuario.NovaSenha));
        await historico.AuditarAsync(operadorId, null, "Usuário salvo", usuario.Login);
    });
    public Task<List<Usuario>> UsuariosAsync() => usuarios.UsuariosAsync();
    public Task<Usuario?> ObterUsuarioAsync(int id) => usuarios.ObterUsuarioAsync(id);
}
