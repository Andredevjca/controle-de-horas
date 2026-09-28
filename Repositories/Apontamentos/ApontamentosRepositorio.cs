using ControleHoras.Data;
using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using Dapper;

namespace ControleHoras.Repositories;

public class ApontamentosRepositorio(SessaoBanco sessao) : IApontamentosRepositorio
{
    public Task<List<Apontamento>> ApontamentosAsync(int usuarioId) => sessao.ListarAsync<Apontamento>("SELECT a.*,d.titulo,d.status FROM apontamentos a JOIN demandas d ON d.id=a.demanda_id WHERE a.usuario_id=@usuarioId ORDER BY a.inicio DESC", new { usuarioId });

    public Task<int> InserirApontamentoAsync(int usuarioId, int demandaId, DateTime inicio, DateTime? fim, string? descricao, bool manual) => sessao.ExecutarAsync("INSERT INTO apontamentos(usuario_id,demanda_id,inicio,fim,descricao,lancamento_manual) VALUES(@usuarioId,@demandaId,@inicio,@fim,@descricao,@manual)", new { usuarioId, demandaId, inicio, fim, descricao, manual });

    public Task<int> FecharApontamentoAsync(int id, DateTime fim) => sessao.ExecutarAsync("UPDATE apontamentos SET fim=@fim WHERE id=@id AND fim IS NULL", new { id, fim });

    public Task<int> RegistrarPausaAsync(int apontamentoId, DateTime inicio) => sessao.ExecutarAsync("INSERT INTO pausas_apontamentos(apontamento_id,inicio) VALUES(@apontamentoId,@inicio)", new { apontamentoId, inicio });

    public Task<int> FecharPausasAsync(int demandaId, DateTime fim) => sessao.ExecutarAsync("UPDATE pausas_apontamentos p JOIN apontamentos a ON a.id=p.apontamento_id SET p.fim=@fim WHERE a.demanda_id=@demandaId AND p.fim IS NULL", new { demandaId, fim });
}
