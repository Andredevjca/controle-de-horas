using System.Net;
using System.Text.RegularExpressions;

static class HttpChecks
{
    public static async Task RunAsync()
    {
        using var handler = new HttpClientHandler { CookieContainer = new(), AllowAutoRedirect = false };
        using var http = new HttpClient(handler) { BaseAddress = new("http://127.0.0.1:5199") };
        void Check(bool valid, string name) { if (!valid) throw new Exception(name); Console.WriteLine("OK " + name); }
        var page = await http.GetAsync("/WhatsApp?Inicio=2026-09-01&Fim=2026-09-30&DemandaId=7&Status=Finalizada");
        var html = await page.Content.ReadAsStringAsync();
        Check(page.IsSuccessStatusCode && html.Contains("Cadastrar cliente e template") && html.Contains("Histórico de envios"), "Tela WhatsApp renderizada com cadastro e histórico");
        var token = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        Check(token.Length > 0, "Formulários têm token antiforgery");
        var missingToken = await http.PostAsync("/WhatsApp/Conectar", new FormUrlEncodedContent([]));
        Check(missingToken.StatusCode == HttpStatusCode.BadRequest, "Conectar rejeita POST sem antiforgery");
        var fields = new Dictionary<string,string> { ["__RequestVerificationToken"]=token, ["Nome"]="Cliente HTTP", ["Telefone"]="(11) 99999-9999", ["Template"]="Olá {cliente}! {horas} de {inicio} a {fim}.", ["Filtro.Inicio"]="2026-09-01", ["Filtro.Fim"]="2026-09-30" };
        var saved = await http.PostAsync("/WhatsApp/SalvarContato", new FormUrlEncodedContent(fields));
        Check(saved.StatusCode == HttpStatusCode.Redirect, "Cadastro do cliente via formulário funciona");
        var values = await http.GetStringAsync("/Relatorios/Valores?Inicio=2026-09-01&Fim=2026-09-30");
        Check(values.Contains("Enviar pelo WhatsApp"), "Valores exibe acesso ao WhatsApp");
        foreach (var tipo in new[] { "pdf", "excel" })
        {
            var data = new Dictionary<string,string> { ["__RequestVerificationToken"]=token, ["ContatoId"]="1", ["Tipo"]=tipo, ["Chave"]=Guid.NewGuid().ToString(), ["Filtro.Inicio"]="2026-09-01", ["Filtro.Fim"]="2026-09-30" };
            var sent = await http.PostAsync("/WhatsApp/Enviar", new FormUrlEncodedContent(data));
            Check(sent.StatusCode == HttpStatusCode.Redirect && sent.Headers.Location!.ToString().Contains("Detalhes"), "Envio simulado " + tipo + " redireciona para histórico");
            var repeated = await http.PostAsync("/WhatsApp/Enviar", new FormUrlEncodedContent(data));
            Check(repeated.Headers.Location == sent.Headers.Location, "POST repetido " + tipo + " reutiliza histórico");
            var detail = await http.GetStringAsync(sent.Headers.Location);
            Check(detail.Contains("Status: Aceito") && detail.Contains("Cliente HTTP"), "Detalhes exibem destinatário e status " + tipo);
            var match = Regex.Match(detail, "href=\"(/WhatsApp/Arquivo/[^\"]+)\"");
            var download = await http.GetAsync(WebUtility.HtmlDecode(match.Groups[1].Value));
            var bytes = await download.Content.ReadAsByteArrayAsync();
            Check(download.IsSuccessStatusCode && (tipo == "pdf" ? bytes.Take(4).SequenceEqual("%PDF"u8.ToArray()) : bytes.Take(2).SequenceEqual("PK"u8.ToArray())), "Download do anexo histórico " + tipo + " tem conteúdo válido");
        }
        var invalid = await http.GetAsync("/WhatsApp/Arquivo/999999");
        Check(invalid.StatusCode == HttpStatusCode.NotFound, "Arquivo inexistente responde 404");
        Console.WriteLine("Fluxos HTTP concluídos com gateway simulado. Nenhuma mensagem real enviada.");
    }
}
