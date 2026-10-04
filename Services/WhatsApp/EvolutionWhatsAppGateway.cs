using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ControleHoras.Interfaces.Services;
using ControleHoras.Models;

namespace ControleHoras.Services;

// Same Evolution endpoints and WHATSAPP-BAILEYS connection used by disparo-whatsapp.
public sealed class EvolutionWhatsAppGateway : IWhatsAppGateway
{
    private readonly HttpClient http;
    private readonly string apiKey;
    public string Prefixo { get; }
    public bool Configurado { get; }
    public EvolutionWhatsAppGateway(HttpClient http, IConfiguration config)
    {
        this.http = http;
        apiKey = config["Evolution:ApiKey"] ?? "";
        Prefixo = config["Evolution:InstancePrefix"] ?? "controle-horas";
        if (!Regex.IsMatch(Prefixo, "^[a-zA-Z0-9-]{1,60}$"))
            throw new InvalidOperationException("Prefixo de instância WhatsApp inválido.");
        Configurado = Uri.TryCreate(config["Evolution:Url"], UriKind.Absolute, out var url)
            && (url.Scheme == "https" || url.Scheme == "http") && !string.IsNullOrWhiteSpace(apiKey);
        if (Configurado)
        {
            http.BaseAddress = new Uri(url!.AbsoluteUri.TrimEnd('/') + "/");
            http.DefaultRequestHeaders.TryAddWithoutValidation("apikey", apiKey);
        }
        http.Timeout = TimeSpan.FromSeconds(60);
    }
    private void ExigirConfiguracao()
    {
        if (!Configurado) throw new InvalidOperationException("Configure a URL e a chave da Evolution no servidor para conectar o WhatsApp.");
    }
    private static string? Texto(JsonElement e, string chave) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(chave, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    public static ContaWhatsApp LerConta(JsonElement raiz, string instancia)
    {
        var detalhe = raiz.TryGetProperty("instance", out var i) && i.ValueKind == JsonValueKind.Object ? i : raiz;
        var estado = Texto(raiz, "connectionStatus") ?? Texto(detalhe, "state") ?? Texto(detalhe, "status") ?? Texto(raiz, "status");
        var qr = Texto(raiz, "base64") ?? Texto(raiz, "qrCode") ?? Texto(raiz, "qrcode") ?? Texto(raiz, "pairingCode");
        if (raiz.TryGetProperty("qrcode", out var q) && q.ValueKind == JsonValueKind.Object)
            qr = Texto(q, "base64") ?? Texto(q, "pairingCode") ?? qr;
        return new(instancia, estado?.ToLowerInvariant() is "open" or "connected",
            Texto(detalhe, "ownerJid") ?? Texto(detalhe, "owner") ?? Texto(raiz, "ownerJid"), qr);
    }
    private async Task<JsonDocument> LerAsync(HttpResponseMessage resposta)
    {
        if (!resposta.IsSuccessStatusCode)
            throw new InvalidOperationException($"A Evolution não concluiu a operação (HTTP {(int)resposta.StatusCode}). Tente atualizar a conexão.");
        return JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
    }
    private async Task<ContaWhatsApp?> LocalizarAsync(string instancia)
    {
        ExigirConfiguracao();
        using var resposta = await http.GetAsync("instance/fetchInstances");
        using var doc = await LerAsync(resposta);
        var raiz = doc.RootElement;
        if (raiz.ValueKind == JsonValueKind.Object && raiz.TryGetProperty("value", out var lista)) raiz = lista;
        var itens = raiz.ValueKind == JsonValueKind.Array ? raiz.EnumerateArray().ToArray() : [raiz];
        foreach (var item in itens.Where(e => e.ValueKind == JsonValueKind.Object))
        {
            var detalhe = item.TryGetProperty("instance", out var i) && i.ValueKind == JsonValueKind.Object ? i : item;
            var nome = Texto(detalhe, "instanceName") ?? Texto(detalhe, "name") ?? Texto(item, "instance");
            if (string.Equals(nome, instancia, StringComparison.OrdinalIgnoreCase)) return LerConta(item, instancia);
        }
        return null;
    }
    public async Task<ContaWhatsApp> StatusAsync(string instancia) => await LocalizarAsync(instancia) ?? new(instancia, false);
    public async Task<ContaWhatsApp> ConectarAsync(string instancia)
    {
        var conta = await LocalizarAsync(instancia);
        if (conta?.Conectado == true) return conta;
        if (conta is null)
        {
            using var criada = await http.PostAsJsonAsync("instance/create", new { instanceName = instancia, token = apiKey, qrcode = true, integration = "WHATSAPP-BAILEYS", syncFullHistory = true, alwaysOnline = true });
            using var documento = await LerAsync(criada);
            conta = LerConta(documento.RootElement, instancia);
            if (conta.Conectado || !string.IsNullOrWhiteSpace(conta.Qrcode)) return conta;
        }
        using var resposta = await http.GetAsync($"instance/connect/{Uri.EscapeDataString(instancia)}");
        using var doc = await LerAsync(resposta);
        return LerConta(doc.RootElement, instancia);
    }
    public async Task DesconectarAsync(string instancia)
    {
        ExigirConfiguracao();
        using var resposta = await http.PostAsync($"instance/logout/{Uri.EscapeDataString(instancia)}", null);
        if (!resposta.IsSuccessStatusCode) throw new InvalidOperationException("Não foi possível desconectar o WhatsApp.");
    }
    public async Task<ResultadoWhatsApp> EnviarAsync(EnvioRelatorioWhatsApp envio)
    {
        ExigirConfiguracao();
        // Do not retry this request: an interrupted response may already have sent the document.
        try
        {
            using var resposta = await http.PostAsJsonAsync($"message/sendMedia/{Uri.EscapeDataString(envio.Instancia)}", new
            {
                number = envio.Telefone, mediatype = "document", mimetype = envio.Mime,
                caption = envio.Mensagem, media = Convert.ToBase64String(envio.Arquivo), fileName = envio.NomeArquivo
            });
            if (!resposta.IsSuccessStatusCode)
                return new((int)resposta.StatusCode >= 500 ? "Incerto" : "Falhou", Erro: $"Evolution respondeu HTTP {(int)resposta.StatusCode}. Confira a conversa antes de tentar novamente.");
            using var doc = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var raiz = doc.RootElement;
            string? id = Texto(raiz, "messageID");
            if (raiz.TryGetProperty("key", out var key)) id ??= Texto(key, "id");
            if (raiz.TryGetProperty("message", out var msg)) id ??= Texto(msg, "id");
            return new("Aceito", id);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new("Incerto", Erro: "Não foi possível confirmar a resposta do WhatsApp. Confira a conversa antes de repetir o envio.");
        }
    }
}
