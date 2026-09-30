using System.Text.RegularExpressions;
using ControleHoras.Models;
using Dapper;
using Microsoft.AspNetCore.Identity;
using MySqlConnector;

namespace ControleHoras.Data;
public class BancoDados(IConfiguration configuracao, IPasswordHasher<Usuario> senhas, IWebHostEnvironment ambiente)
{
    public MySqlConnection Conectar() => new(configuracao.GetConnectionString("BancoDados") ?? throw new InvalidOperationException("Configure a conexão BancoDados."));
    public async Task InicializarAsync()
    {
        var conexao = new MySqlConnectionStringBuilder(configuracao.GetConnectionString("BancoDados") ?? throw new InvalidOperationException("Configure a conexão BancoDados."));
        var nome = conexao.Database;
        if (!Regex.IsMatch(nome, "^[a-zA-Z0-9_]+$")) throw new InvalidOperationException("Nome do banco invÃ¡lido.");
        conexao.Database = "";
        await using var servidor = new MySqlConnection(conexao.ConnectionString);
        await servidor.ExecuteAsync($"CREATE DATABASE IF NOT EXISTS `{nome}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
        await using var banco = Conectar();
        await banco.ExecuteAsync(await File.ReadAllTextAsync(Path.Combine(ambiente.ContentRootPath, "Data", "estrutura.sql")));
        if (await banco.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='pagamentos' AND COLUMN_NAME='demanda_id'") == 0)
            await banco.ExecuteAsync("ALTER TABLE pagamentos ADD COLUMN demanda_id INT NULL, ADD CONSTRAINT fk_pagamentos_demanda FOREIGN KEY (demanda_id) REFERENCES demandas(id)");
        if (await banco.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM usuarios") == 0)
        {
            // Credenciais padrao para o primeiro acesso em um banco sem usuarios.
            var login = configuracao["Administrador:Login"];
            var nomeAdministrador = configuracao["Administrador:Nome"];
            var senha = configuracao["Administrador:SenhaInicial"];
            if (string.IsNullOrWhiteSpace(login) && string.IsNullOrWhiteSpace(nomeAdministrador) && string.IsNullOrWhiteSpace(senha))
            {
                login = "andre";
                nomeAdministrador = "Andre";
                senha = "12345";
            }
            else if (string.IsNullOrWhiteSpace(login) || login.Length > 60 || string.IsNullOrWhiteSpace(nomeAdministrador) || nomeAdministrador.Length > 120 || string.IsNullOrWhiteSpace(senha) || senha.Length < 12)
                throw new InvalidOperationException("Configure Administrador:Login, Administrador:Nome e Administrador:SenhaInicial (ao menos 12 caracteres) para cadastrar o primeiro administrador.");
            var usuario = new Usuario { Nome = nomeAdministrador, Login = login };
            await banco.ExecuteAsync("INSERT INTO usuarios(nome,usuario,senha,ativo,administrador) VALUES(@Nome,@Login,@Senha,TRUE,TRUE)",
                new { usuario.Nome, usuario.Login, Senha = senhas.HashPassword(usuario, senha) });
        }
    }
}
