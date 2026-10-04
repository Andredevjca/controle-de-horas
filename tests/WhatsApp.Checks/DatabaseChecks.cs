using ControleHoras.Data;
using ControleHoras.Models;
using ControleHoras.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

static class DatabaseChecks
{
    public static async Task RunAsync(string root)
    {
        var config = new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Local.json", true).AddEnvironmentVariables().Build();
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        await using var c = new MySqlConnection(config.GetConnectionString("BancoDados"));
        var banco = new BancoDados(config, null!, null!);
        var repo = new WhatsAppRepositorio(banco);
        var teste = "wa-check-" + Guid.NewGuid().ToString("N");
        int usuario = 0;
        void Check(bool valid, string name) { if (!valid) throw new Exception(name); Console.WriteLine("OK " + name); }
        try
        {
            // Disabled, temporary test identity. No existing account is edited and no provider is called.
            usuario = await c.ExecuteScalarAsync<int>("INSERT INTO usuarios(nome,usuario,senha,ativo,administrador) VALUES('Teste temporário WhatsApp',@teste,'SEM_LOGIN',FALSE,FALSE); SELECT LAST_INSERT_ID();", new { teste });
            await repo.SalvarContatoAsync(usuario, new() { Nome = "Cliente de teste", Telefone = "5511999999999", Template = "Olá {cliente}" });
            var contato = (await repo.ContatosAsync(usuario)).Single();
            Check(contato.Nome == "Cliente de teste" && contato.Template == "Olá {cliente}", "Cadastro e template persistidos no MySQL");
            contato.Nome = "Cliente editado"; await repo.SalvarContatoAsync(usuario, contato);
            Check((await repo.ContatoAsync(usuario, contato.Id))!.Nome == "Cliente editado", "Edição persistida");
            Check(await repo.ContatoAsync(0, contato.Id) is null, "Contato isolado por usuário");
            var instancia = await repo.InstanciaAsync(usuario, "test-wa");
            Check(await repo.InstanciaAsync(usuario, "outro-prefixo") == instancia, "Instância do usuário preservada");
            var envio = new EnvioRelatorioWhatsApp { UsuarioId = usuario, Chave = Guid.NewGuid(), Nome = contato.Nome, Telefone = contato.Telefone, Template = contato.Template, Mensagem = "Olá Cliente editado", Instancia = instancia, Tipo = "pdf", Inicio = DateTime.Today, Fim = DateTime.Today, NomeArquivo = "teste.pdf", Mime = "application/pdf", Arquivo = [37,80,68,70,45] };
            var (registrado, novo) = await repo.ReservarAsync(envio);
            var (repetido, novoRepetido) = await repo.ReservarAsync(envio);
            Check(novo && !novoRepetido && registrado.Id == repetido.Id, "Chave única impede segunda reserva no MySQL");
            await repo.FinalizarAsync(usuario, registrado.Id, new("Aceito", "protocolo-simulado"));
            var obtido = await repo.ObterEnvioAsync(usuario, registrado.Id, true);
            Check(obtido!.Arquivo.SequenceEqual(envio.Arquivo) && obtido.EvolutionId == "protocolo-simulado" && obtido.Status == "Aceito", "Anexo, resultado e protocolo recuperados do banco");
            Check(await repo.ObterEnvioAsync(0, registrado.Id, true) is null, "Anexo não acessível por outro usuário");
            var historico = await repo.HistoricoAsync(usuario, 1);
            Check(historico.Total == 1 && historico.Itens.Single().Arquivo.Length == 0, "Histórico paginado carrega metadados sem o BLOB");
        }
        finally
        {
            if (usuario > 0)
            {
                // Cleanup is limited to the identity inserted above, verified by its unique test login.
                var pertence = await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM usuarios WHERE id=@usuario AND usuario=@teste AND ativo=FALSE", new { usuario, teste });
                if (pertence == 1)
                    await c.ExecuteAsync("DELETE FROM whatsapp_envios WHERE usuario_id=@usuario; DELETE FROM whatsapp_contatos WHERE usuario_id=@usuario; DELETE FROM whatsapp_conexoes WHERE usuario_id=@usuario; DELETE FROM usuarios WHERE id=@usuario AND usuario=@teste", new { usuario, teste });
            }
        }
        Console.WriteLine("Verificação MySQL concluída; registros temporários removidos. Nenhuma mensagem enviada.");
    }
}
