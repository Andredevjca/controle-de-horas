using ControleHoras.Interfaces.Repositories;
using ControleHoras.Data;
using ControleHoras.Models;
using Dapper;
using MySqlConnector;

namespace ControleHoras.Repositories;
public class Repositorio(BancoDados banco) : IControleRepositorio, IDisposable
{
    private readonly MySqlConnection conexao = banco.Conectar();
    private MySqlTransaction? transacao;
    public void Dispose() => conexao.Dispose();
    private Task<int> Executar(string sql, object? parametros = null) => conexao.ExecuteAsync(sql, parametros, transacao);
    private async Task<List<T>> Listar<T>(string sql, object? parametros = null) => (await conexao.QueryAsync<T>(sql, parametros, transacao)).ToList();
    public async Task TransacionarAsync(int usuarioId, Func<Task> operacao)
    {
        await conexao.OpenAsync();
        await using var atual = await conexao.BeginTransactionAsync();
        transacao = atual;
        try
        {
            await conexao.ExecuteScalarAsync<int>("SELECT id FROM usuarios WHERE id=@usuarioId FOR UPDATE", new { usuarioId }, atual);
            await operacao();
            await atual.CommitAsync();
        }
        catch { await atual.RollbackAsync(); throw; }
        finally { transacao = null; await conexao.CloseAsync(); }
    }
    public async Task<Usuario?> ObterUsuarioAsync(int id) => (await Listar<Usuario>("SELECT *,usuario AS Login FROM usuarios WHERE id=@id", new { id })).FirstOrDefault();
    public async Task<Usuario?> BuscarLoginAsync(string login) => (await Listar<Usuario>("SELECT *,usuario AS Login FROM usuarios WHERE usuario=@login", new { login })).FirstOrDefault();
    public Task<List<Usuario>> UsuariosAsync() => Listar<Usuario>("SELECT *,usuario AS Login FROM usuarios ORDER BY nome");
    public async Task SalvarUsuarioAsync(EdicaoUsuario usuario, string? senha)
    {
        if (usuario.Id == 0)
            await Executar("INSERT INTO usuarios(nome,usuario,email,senha,ativo) VALUES(@Nome,@Login,@Email,@senha,@Ativo)", new { usuario.Nome, usuario.Login, usuario.Email, senha, usuario.Ativo });
        else
            await Executar("UPDATE usuarios SET nome=@Nome,usuario=@Login,email=@Email,ativo=@Ativo,senha=COALESCE(@senha,senha),versao_sessao=versao_sessao+1,data_alteracao=NOW() WHERE id=@Id", new { usuario.Nome, usuario.Login, usuario.Email, usuario.Ativo, senha, usuario.Id });
    }
    public Task<List<Demanda>> DemandasAsync(int usuarioId) => Listar<Demanda>("""
        SELECT d.*,p.nome AS projeto_nome,COALESCE((SELECT SUM(TIMESTAMPDIFF(SECOND,a.inicio,COALESCE(a.fim,UTC_TIMESTAMP()))) FROM apontamentos a WHERE a.demanda_id=d.id),0) AS segundos
        FROM demandas d LEFT JOIN projetos p ON p.id=d.projeto_id WHERE d.usuario_id=@usuarioId ORDER BY d.id DESC
        """, new { usuarioId });
    public Task<List<Projeto>> ProjetosAsync(int usuarioId) => Listar<Projeto>("SELECT id,nome FROM projetos WHERE usuario_id=@usuarioId ORDER BY nome", new { usuarioId });
    public Task<int> CriarProjetoAsync(int usuarioId, string nome) => Executar("INSERT INTO projetos(usuario_id,nome) VALUES(@usuarioId,@nome)", new { usuarioId, nome });
    public async Task<int> SalvarDemandaAsync(Demanda demanda)
    {
        if (demanda.Id == 0)
        {
            return await conexao.ExecuteScalarAsync<int>("""
                INSERT INTO demandas(usuario_id,projeto_id,titulo,descricao,prioridade,status,data_recebimento,prazo,observacoes)
                VALUES(@UsuarioId,@ProjetoId,@Titulo,@Descricao,@Prioridade,@Status,@DataRecebimento,@Prazo,@Observacoes); SELECT LAST_INSERT_ID();
                """, demanda, transacao);
        }
        await Executar("""
            UPDATE demandas SET projeto_id=@ProjetoId,titulo=@Titulo,descricao=@Descricao,prioridade=@Prioridade,
            data_recebimento=@DataRecebimento,prazo=@Prazo,observacoes=@Observacoes WHERE id=@Id AND usuario_id=@UsuarioId
            """, demanda);
        return demanda.Id;
    }
    public Task<int> AtualizarStatusAsync(int usuarioId, int id, string status, DateTime? finalizacao) => Executar("UPDATE demandas SET status=@status,data_finalizacao=@finalizacao WHERE id=@id AND usuario_id=@usuarioId", new { usuarioId, id, status, finalizacao });
    public Task<List<Apontamento>> ApontamentosAsync(int usuarioId) => Listar<Apontamento>("SELECT a.*,d.titulo,d.status FROM apontamentos a JOIN demandas d ON d.id=a.demanda_id WHERE a.usuario_id=@usuarioId ORDER BY a.inicio DESC", new { usuarioId });
    public Task<int> InserirApontamentoAsync(int usuarioId, int demandaId, DateTime inicio, DateTime? fim, string? descricao, bool manual) => Executar("INSERT INTO apontamentos(usuario_id,demanda_id,inicio,fim,descricao,lancamento_manual) VALUES(@usuarioId,@demandaId,@inicio,@fim,@descricao,@manual)", new { usuarioId, demandaId, inicio, fim, descricao, manual });
    public Task<int> FecharApontamentoAsync(int id, DateTime fim) => Executar("UPDATE apontamentos SET fim=@fim WHERE id=@id AND fim IS NULL", new { id, fim });
    public Task<int> RegistrarPausaAsync(int apontamentoId, DateTime inicio) => Executar("INSERT INTO pausas_apontamentos(apontamento_id,inicio) VALUES(@apontamentoId,@inicio)", new { apontamentoId, inicio });
    public Task<int> FecharPausasAsync(int demandaId, DateTime fim) => Executar("UPDATE pausas_apontamentos p JOIN apontamentos a ON a.id=p.apontamento_id SET p.fim=@fim WHERE a.demanda_id=@demandaId AND p.fim IS NULL", new { demandaId, fim });
    public async Task<decimal?> ValorHoraAsync(int usuarioId) => await conexao.QuerySingleOrDefaultAsync<decimal?>("SELECT valor_hora FROM configuracoes WHERE usuario_id=@usuarioId", new { usuarioId }, transacao);
    public Task<int> DefinirValorAsync(int usuarioId, decimal? valor) => Executar("INSERT INTO configuracoes(usuario_id,valor_hora) VALUES(@usuarioId,@valor) ON DUPLICATE KEY UPDATE valor_hora=@valor", new { usuarioId, valor });
    public Task<List<Pagamento>> PagamentosAsync(int usuarioId) => Listar<Pagamento>("SELECT * FROM pagamentos WHERE usuario_id=@usuarioId ORDER BY data_pagamento DESC,id DESC", new { usuarioId });
    public Task<int> InserirPagamentoAsync(int usuarioId, Pagamento pagamento) => Executar("INSERT INTO pagamentos(usuario_id,data_pagamento,periodo_inicio,periodo_fim,valor_pago,observacao) VALUES(@usuarioId,@DataPagamento,@PeriodoInicio,@PeriodoFim,@ValorPago,@Observacao)", new { usuarioId, pagamento.DataPagamento, pagamento.PeriodoInicio, pagamento.PeriodoFim, pagamento.ValorPago, pagamento.Observacao });
    public Task<int> AuditarAsync(int usuarioId, int? demandaId, string acao, string? detalhes = null) => Executar("INSERT INTO historico_demandas(usuario_id,demanda_id,data,acao,detalhes) VALUES(@usuarioId,@demandaId,UTC_TIMESTAMP(),@acao,@detalhes)", new { usuarioId, demandaId, acao, detalhes });
    public Task<List<RegistroHistorico>> HistoricoAsync(int usuarioId) => Listar<RegistroHistorico>("SELECT data,acao,detalhes,demanda_id FROM historico_demandas WHERE usuario_id=@usuarioId ORDER BY id DESC LIMIT 100", new { usuarioId });
}
