using System.Text.RegularExpressions;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using ControleHoras.Models;

namespace ControleHoras.Services;

public sealed class WhatsAppRelatoriosServico(IWhatsAppRepositorio repositorio, IWhatsAppGateway gateway,
    IRelatoriosServico relatorios, ILogger<WhatsAppRelatoriosServico> logger)
{
    public bool Configurado => gateway.Configurado;
    public static string NormalizarTelefone(string telefone)
    {
        var numero = Regex.Replace(telefone ?? "", "[^0-9]", "").TrimStart('0');
        if (numero.Length is 10 or 11) numero = "55" + numero;
        if (numero.Length is < 12 or > 13 || !numero.StartsWith("55"))
            throw new InvalidOperationException("Informe um telefone brasileiro com DDD, por exemplo: 5511999999999.");
        return numero;
    }
    public static string Mensagem(string template, string cliente, Painel painel)
    {
        var valores = new Dictionary<string, string>
        {
            ["cliente"] = cliente, ["inicio"] = painel.Filtro.Inicio.ToString("dd/MM/yyyy"),
            ["fim"] = painel.Filtro.Fim.ToString("dd/MM/yyyy"), ["horas"] = Formato.Horas(painel.Segundos),
            ["valor"] = Formato.Dinheiro(painel.Estimado), ["saldo"] = Formato.Dinheiro(painel.Saldo)
        };
        return Regex.Replace(template, @"\{([^{}]+)\}", m => valores.TryGetValue(m.Groups[1].Value, out var valor)
            ? valor : throw new InvalidOperationException($"Variável de mensagem desconhecida: {m.Value}"));
    }
    public async Task SalvarContatoAsync(int usuarioId, ContatoWhatsApp contato)
    {
        contato.Nome = contato.Nome.Trim(); contato.Template = contato.Template.Trim();
        if (contato.Nome.Length is < 1 or > 120 || contato.Template.Length is < 1 or > 1000 || contato.Id < 0)
            throw new InvalidOperationException("Preencha o nome e uma mensagem de até 1.000 caracteres.");
        contato.Telefone = NormalizarTelefone(contato.Telefone);
        _ = Mensagem(contato.Template, contato.Nome, new Painel());
        await repositorio.SalvarContatoAsync(usuarioId, contato);
    }
    public async Task<ContaWhatsApp> StatusAsync(int usuarioId) => await gateway.StatusAsync(await repositorio.InstanciaAsync(usuarioId, gateway.Prefixo));
    public async Task<ContaWhatsApp> ConectarAsync(int usuarioId) => await gateway.ConectarAsync(await repositorio.InstanciaAsync(usuarioId, gateway.Prefixo));
    public async Task DesconectarAsync(int usuarioId) => await gateway.DesconectarAsync(await repositorio.InstanciaAsync(usuarioId, gateway.Prefixo));

    public async Task<long> EnviarAsync(int usuarioId, string? nomeUsuario, EnviarRelatorioWhatsApp pedido)
    {
        if (pedido.Chave == Guid.Empty || pedido.Tipo is not ("pdf" or "excel")) throw new InvalidOperationException("Selecione PDF ou Excel e atualize o formulário.");
        if (!Configurado) throw new InvalidOperationException("Configure a integração WhatsApp antes de enviar.");
        var contato = await repositorio.ContatoAsync(usuarioId, pedido.ContatoId) ?? throw new InvalidOperationException("Contato não encontrado.");
        var painel = await relatorios.PainelAsync(usuarioId, pedido.Filtro);
        var mensagem = Mensagem(contato.Template, contato.Nome, painel);
        if (mensagem.Length > 1024) throw new InvalidOperationException("A mensagem preenchida ultrapassa 1.024 caracteres. Reduza o template do cliente.");
        var arquivo = pedido.Tipo == "pdf" ? relatorios.GerarPdf(painel, nomeUsuario) : relatorios.GerarExcel(painel, nomeUsuario);
        if (arquivo.Length > 10 * 1024 * 1024) throw new InvalidOperationException("O relatório ultrapassa 10 MB. Selecione um período menor.");
        var envio = new EnvioRelatorioWhatsApp
        {
            UsuarioId = usuarioId, Chave = pedido.Chave, Nome = contato.Nome, Telefone = NormalizarTelefone(contato.Telefone),
            Template = contato.Template, Mensagem = mensagem,
            Instancia = await repositorio.InstanciaAsync(usuarioId, gateway.Prefixo), Tipo = pedido.Tipo,
            Inicio = painel.Filtro.Inicio, Fim = painel.Filtro.Fim, DemandaId = painel.Filtro.DemandaId, FiltroStatus = painel.Filtro.Status,
            NomeArquivo = $"horas-{painel.Filtro.Inicio:yyyyMMdd}-{painel.Filtro.Fim:yyyyMMdd}.{(pedido.Tipo == "pdf" ? "pdf" : "xlsx")}",
            Mime = pedido.Tipo == "pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Arquivo = arquivo
        };
        var reserva = await repositorio.ReservarAsync(envio);
        if (!reserva.Novo) return reserva.Envio.Id;
        ResultadoWhatsApp resultado;
        try
        {
            var conta = await gateway.StatusAsync(envio.Instancia);
            resultado = conta.Conectado ? await gateway.EnviarAsync(envio) : new("Falhou", Erro: "WhatsApp desconectado. Conecte pelo QR Code antes de enviar.");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Falha no envio WhatsApp {EnvioId}", envio.Id);
            resultado = new("Incerto", Erro: "Não foi possível confirmar o envio. Confira a conexão e a conversa antes de tentar novamente.");
        }
        try { await repositorio.FinalizarAsync(usuarioId, envio.Id, resultado); }
        catch (Exception e)
        {
            // Reservation remains durable. A repeated POST with the same key will never resend.
            logger.LogError(e, "Não foi possível atualizar o resultado do envio WhatsApp {EnvioId}", envio.Id);
            throw new InvalidOperationException("O envio foi registrado, mas seu resultado não pôde ser salvo. Consulte o histórico e a conversa antes de repetir.");
        }
        return envio.Id;
    }
}
