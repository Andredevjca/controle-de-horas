using ControleHoras.Data;
using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using Dapper;

namespace ControleHoras.Repositories;

public class UsuariosRepositorio(SessaoBanco sessao) : IUsuariosRepositorio
{
    public async Task<Usuario?> ObterUsuarioAsync(int id) => (await sessao.ListarAsync<Usuario>("SELECT *,usuario AS Login FROM usuarios WHERE id=@id", new { id })).FirstOrDefault();

    public async Task<Usuario?> BuscarLoginAsync(string login) => (await sessao.ListarAsync<Usuario>("SELECT *,usuario AS Login FROM usuarios WHERE usuario=@login", new { login })).FirstOrDefault();

    public Task<List<Usuario>> UsuariosAsync() => sessao.ListarAsync<Usuario>("SELECT *,usuario AS Login FROM usuarios ORDER BY nome");

    public async Task SalvarUsuarioAsync(EdicaoUsuario usuario, string? senha)
    {
        if (usuario.Id == 0)
            await sessao.ExecutarAsync("INSERT INTO usuarios(nome,usuario,email,senha,ativo) VALUES(@Nome,@Login,@Email,@senha,@Ativo)", new { usuario.Nome, usuario.Login, usuario.Email, senha, usuario.Ativo });
        else
            await sessao.ExecutarAsync("UPDATE usuarios SET nome=@Nome,usuario=@Login,email=@Email,ativo=@Ativo,senha=COALESCE(@senha,senha),versao_sessao=versao_sessao+1,data_alteracao=NOW() WHERE id=@Id", new { usuario.Nome, usuario.Login, usuario.Email, usuario.Ativo, senha, usuario.Id });
    }
}
