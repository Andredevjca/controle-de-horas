using ControleHoras.Interfaces.Repositories;
using Dapper;
using MySqlConnector;

namespace ControleHoras.Data;

// Uma instância por requisição mantém todos os repositórios na mesma transação.
public sealed class SessaoBanco(BancoDados banco) : IUnidadeTrabalho, IDisposable
{
    public MySqlConnection Conexao { get; } = banco.Conectar();
    public MySqlTransaction? Transacao { get; private set; }
    public void Dispose() => Conexao.Dispose();
    public Task<int> ExecutarAsync(string sql, object? parametros = null) => Conexao.ExecuteAsync(sql, parametros, Transacao);
    public async Task<List<T>> ListarAsync<T>(string sql, object? parametros = null) => (await Conexao.QueryAsync<T>(sql, parametros, Transacao)).ToList();
    public async Task TransacionarAsync(int usuarioId, Func<Task> operacao)
    {
        await Conexao.OpenAsync();
        await using var atual = await Conexao.BeginTransactionAsync();
        Transacao = atual;
        try
        {
            await Conexao.ExecuteScalarAsync<int>("SELECT id FROM usuarios WHERE id=@usuarioId FOR UPDATE", new { usuarioId }, atual);
            await operacao();
            await atual.CommitAsync();
        }
        catch { await atual.RollbackAsync(); throw; }
        finally { Transacao = null; await Conexao.CloseAsync(); }
    }
}
