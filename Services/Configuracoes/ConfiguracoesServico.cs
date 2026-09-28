using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using static ControleHoras.Services.Validacao;

namespace ControleHoras.Services;

public class ConfiguracoesServico(IUsuariosRepositorio usuarios, IConfiguracoesRepositorio configuracoes, IHistoricoRepositorio historico, IUnidadeTrabalho unidadeTrabalho, IPasswordHasher<Usuario> senhas, IUsuariosServico usuariosServico) : IConfiguracoesServico
{
    public Task DefinirValorAsync(int usuarioId, decimal? valor) => unidadeTrabalho.TransacionarAsync(usuarioId, async () => {
        Exigir(valor == null || valor > 0 && valor <= 99999999.99m, "Informe um valor positivo ou deixe em branco.");
        var anterior = await configuracoes.ValorHoraAsync(usuarioId);
        await configuracoes.DefinirValorAsync(usuarioId, valor);
        await historico.AuditarAsync(usuarioId, null, "Valor/hora alterado", $"{Formato.Dinheiro(anterior)} → {Formato.Dinheiro(valor)}");
    });
    public Task<decimal?> ValorHoraAsync(int usuarioId) => configuracoes.ValorHoraAsync(usuarioId);
    public async Task AlterarSenhaAsync(int usuarioId, string senhaAtual, string novaSenha, string confirmarSenha)
    {
        var usuario = await usuarios.ObterUsuarioAsync(usuarioId);
        Exigir(usuario != null, "Usuário não encontrado.");
        Exigir(!string.IsNullOrEmpty(senhaAtual) && senhas.VerifyHashedPassword(usuario!, usuario!.Senha, senhaAtual) != PasswordVerificationResult.Failed, "Senha atual incorreta.");
        Exigir(!string.IsNullOrEmpty(novaSenha) && novaSenha.Length >= 8 && novaSenha == confirmarSenha, "Use pelo menos 8 caracteres e confirme a nova senha corretamente.");
        await usuariosServico.SalvarUsuarioAsync(usuarioId, new EdicaoUsuario { Id = usuario!.Id, Nome = usuario.Nome, Login = usuario.Login, Email = usuario.Email, Ativo = usuario.Ativo, NovaSenha = novaSenha });
    }
}
