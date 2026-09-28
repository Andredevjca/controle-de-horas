using ControleHoras.Data;
using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using Dapper;

namespace ControleHoras.Repositories;

public class ConfiguracoesRepositorio(SessaoBanco sessao) : IConfiguracoesRepositorio
{
    public async Task<decimal?> ValorHoraAsync(int usuarioId) => await sessao.Conexao.QuerySingleOrDefaultAsync<decimal?>("SELECT valor_hora FROM configuracoes WHERE usuario_id=@usuarioId", new { usuarioId }, sessao.Transacao);

    public Task<int> DefinirValorAsync(int usuarioId, decimal? valor) => sessao.ExecutarAsync("INSERT INTO configuracoes(usuario_id,valor_hora) VALUES(@usuarioId,@valor) ON DUPLICATE KEY UPDATE valor_hora=@valor", new { usuarioId, valor });
}
