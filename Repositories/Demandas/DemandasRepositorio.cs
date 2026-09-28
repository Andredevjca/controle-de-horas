using ControleHoras.Data;
using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using Dapper;

namespace ControleHoras.Repositories;

public class DemandasRepositorio(SessaoBanco sessao) : IDemandasRepositorio
{
    public Task<List<Demanda>> DemandasAsync(int usuarioId) => sessao.ListarAsync<Demanda>("""
        SELECT d.*,p.nome AS projeto_nome,COALESCE((SELECT SUM(TIMESTAMPDIFF(SECOND,a.inicio,COALESCE(a.fim,UTC_TIMESTAMP()))) FROM apontamentos a WHERE a.demanda_id=d.id),0) AS segundos
        FROM demandas d LEFT JOIN projetos p ON p.id=d.projeto_id WHERE d.usuario_id=@usuarioId ORDER BY d.id DESC
        """, new { usuarioId });

    public Task<List<Projeto>> ProjetosAsync(int usuarioId) => sessao.ListarAsync<Projeto>("SELECT id,nome FROM projetos WHERE usuario_id=@usuarioId ORDER BY nome", new { usuarioId });

    public Task<int> CriarProjetoAsync(int usuarioId, string nome) => sessao.ExecutarAsync("INSERT INTO projetos(usuario_id,nome) VALUES(@usuarioId,@nome)", new { usuarioId, nome });

    public async Task<int> SalvarDemandaAsync(Demanda demanda)
    {
        if (demanda.Id == 0)
        {
            return await sessao.Conexao.ExecuteScalarAsync<int>("""
                INSERT INTO demandas(usuario_id,projeto_id,titulo,descricao,prioridade,status,data_recebimento,prazo,observacoes)
                VALUES(@UsuarioId,@ProjetoId,@Titulo,@Descricao,@Prioridade,@Status,@DataRecebimento,@Prazo,@Observacoes); SELECT LAST_INSERT_ID();
                """, demanda, sessao.Transacao);
        }
        await sessao.ExecutarAsync("""
            UPDATE demandas SET projeto_id=@ProjetoId,titulo=@Titulo,descricao=@Descricao,prioridade=@Prioridade,
            data_recebimento=@DataRecebimento,prazo=@Prazo,observacoes=@Observacoes WHERE id=@Id AND usuario_id=@UsuarioId
            """, demanda);
        return demanda.Id;
    }

    public Task<int> AtualizarStatusAsync(int usuarioId, int id, string status, DateTime? finalizacao) => sessao.ExecutarAsync("UPDATE demandas SET status=@status,data_finalizacao=@finalizacao WHERE id=@id AND usuario_id=@usuarioId", new { usuarioId, id, status, finalizacao });
}
