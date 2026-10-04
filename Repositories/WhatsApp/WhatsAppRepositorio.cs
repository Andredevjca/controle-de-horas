using ControleHoras.Data;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Models;
using Dapper;
using MySqlConnector;

namespace ControleHoras.Repositories;

public sealed class WhatsAppRepositorio(BancoDados banco) : IWhatsAppRepositorio
{
    private const string Colunas = "id, usuario_id, chave, nome, telefone, template, mensagem, instancia, tipo, inicio, fim, demanda_id, filtro_status, nome_arquivo, mime, status, evolution_id, erro, criado_em, finalizado_em";

    public async Task<List<ContatoWhatsApp>> ContatosAsync(int usuarioId)
    {
        await using var c = banco.Conectar();
        return (await c.QueryAsync<ContatoWhatsApp>("SELECT id,nome,telefone,template FROM whatsapp_contatos WHERE usuario_id=@usuarioId ORDER BY nome,id", new { usuarioId })).AsList();
    }
    public async Task<ContatoWhatsApp?> ContatoAsync(int usuarioId, int id)
    {
        await using var c = banco.Conectar();
        return await c.QuerySingleOrDefaultAsync<ContatoWhatsApp>("SELECT id,nome,telefone,template FROM whatsapp_contatos WHERE usuario_id=@usuarioId AND id=@id", new { usuarioId, id });
    }
    public async Task SalvarContatoAsync(int usuarioId, ContatoWhatsApp contato)
    {
        await using var c = banco.Conectar();
        var parametros = new { usuarioId, contato.Id, contato.Nome, contato.Telefone, contato.Template };
        if (contato.Id == 0)
            await c.ExecuteAsync("INSERT INTO whatsapp_contatos(usuario_id,nome,telefone,template) VALUES(@usuarioId,@Nome,@Telefone,@Template)", parametros);
        else if (await c.ExecuteAsync("UPDATE whatsapp_contatos SET nome=@Nome,telefone=@Telefone,template=@Template WHERE usuario_id=@usuarioId AND id=@Id", parametros) == 0)
            throw new InvalidOperationException("Contato não encontrado.");
    }
    public async Task<string> InstanciaAsync(int usuarioId, string prefixo)
    {
        await using var c = banco.Conectar();
        await c.ExecuteAsync("INSERT IGNORE INTO whatsapp_conexoes(usuario_id,instancia) VALUES(@usuarioId,@instancia)", new { usuarioId, instancia = $"{prefixo}-{usuarioId}" });
        return await c.QuerySingleAsync<string>("SELECT instancia FROM whatsapp_conexoes WHERE usuario_id=@usuarioId", new { usuarioId });
    }
    public async Task<(EnvioRelatorioWhatsApp Envio, bool Novo)> ReservarAsync(EnvioRelatorioWhatsApp envio)
    {
        await using var c = banco.Conectar();
        try
        {
            envio.Id = await c.ExecuteScalarAsync<long>("""
                INSERT INTO whatsapp_envios(usuario_id,chave,nome,telefone,template,mensagem,instancia,tipo,inicio,fim,demanda_id,filtro_status,nome_arquivo,mime,arquivo,status)
                VALUES(@UsuarioId,@Chave,@Nome,@Telefone,@Template,@Mensagem,@Instancia,@Tipo,@Inicio,@Fim,@DemandaId,@FiltroStatus,@NomeArquivo,@Mime,@Arquivo,'Enviando');
                SELECT LAST_INSERT_ID();
                """, envio);
            return (envio, true);
        }
        catch (MySqlException e) when (e.Number == 1062)
        {
            var existente = await c.QuerySingleAsync<EnvioRelatorioWhatsApp>($"SELECT {Colunas} FROM whatsapp_envios WHERE usuario_id=@UsuarioId AND chave=@Chave", envio);
            return (existente, false);
        }
    }
    public async Task FinalizarAsync(int usuarioId, long id, ResultadoWhatsApp resultado)
    {
        await using var c = banco.Conectar();
        await c.ExecuteAsync("UPDATE whatsapp_envios SET status=@Status,evolution_id=@EvolutionId,erro=@Erro,finalizado_em=UTC_TIMESTAMP() WHERE usuario_id=@usuarioId AND id=@id AND status='Enviando'",
            new { usuarioId, id, resultado.Status, EvolutionId = resultado.Id, resultado.Erro });
    }
    public async Task<EnvioRelatorioWhatsApp?> ObterEnvioAsync(int usuarioId, long id, bool incluirArquivo = false)
    {
        await using var c = banco.Conectar();
        return await c.QuerySingleOrDefaultAsync<EnvioRelatorioWhatsApp>($"SELECT {Colunas}{(incluirArquivo ? ",arquivo" : "")} FROM whatsapp_envios WHERE usuario_id=@usuarioId AND id=@id", new { usuarioId, id });
    }
    public async Task<(List<EnvioRelatorioWhatsApp> Itens, int Total)> HistoricoAsync(int usuarioId, int pagina)
    {
        await using var c = banco.Conectar();
        using var dados = await c.QueryMultipleAsync($"SELECT {Colunas} FROM whatsapp_envios WHERE usuario_id=@usuarioId ORDER BY id DESC LIMIT 20 OFFSET @offset; SELECT COUNT(*) FROM whatsapp_envios WHERE usuario_id=@usuarioId", new { usuarioId, offset = (pagina - 1) * 20 });
        return ((await dados.ReadAsync<EnvioRelatorioWhatsApp>()).AsList(), await dados.ReadSingleAsync<int>());
    }
}
