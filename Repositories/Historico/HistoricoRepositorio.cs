using ControleHoras.Data;
using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using Dapper;

namespace ControleHoras.Repositories;

public class HistoricoRepositorio(SessaoBanco sessao) : IHistoricoRepositorio
{
    public Task<int> AuditarAsync(int usuarioId, int? demandaId, string acao, string? detalhes = null) => sessao.ExecutarAsync("INSERT INTO historico_demandas(usuario_id,demanda_id,data,acao,detalhes) VALUES(@usuarioId,@demandaId,UTC_TIMESTAMP(),@acao,@detalhes)", new { usuarioId, demandaId, acao, detalhes });

    public Task<List<RegistroHistorico>> HistoricoAsync(int usuarioId) => sessao.ListarAsync<RegistroHistorico>("SELECT data,acao,detalhes,demanda_id FROM historico_demandas WHERE usuario_id=@usuarioId ORDER BY id DESC LIMIT 100", new { usuarioId });
}
