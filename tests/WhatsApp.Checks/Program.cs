using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using ControleHoras.Data;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using ControleHoras.Models;
using ControleHoras.Services;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PdfSharp.Pdf.IO;

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
if (!File.Exists(Path.Combine(root, "ControleHoras.csproj"))) root = Directory.GetCurrentDirectory();
if (args.Contains("--preview")) { await Preview.RunAsync(root); return; }
if (args.Contains("--database-checks")) { await DatabaseChecks.RunAsync(root); return; }
if (args.Contains("--http-checks")) { await HttpChecks.RunAsync(); return; }
if (args.Contains("--initialize-database"))
{
    var config = new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Local.json", true).AddEnvironmentVariables().Build();
    await using var connection = new MySqlConnector.MySqlConnection(config.GetConnectionString("BancoDados"));
    try
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(root, "Data/estrutura.sql"));
        // Only the three additive WhatsApp tables. No startup, existing users, or remote WhatsApp mutations.
        await connection.ExecuteAsync(sql[sql.IndexOf("CREATE TABLE IF NOT EXISTS whatsapp_contatos", StringComparison.Ordinal)..]);
        var total = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema=DATABASE() AND table_name IN ('whatsapp_contatos','whatsapp_conexoes','whatsapp_envios')");
        Check(total == 3, "Três tabelas WhatsApp disponíveis no banco configurado");
    }
    catch (MySqlConnector.MySqlException e) { Console.WriteLine($"Banco indisponível para inicialização: MySQL {e.Number}."); Environment.ExitCode = 2; }
    return;
}

static void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("OK " + name); }
static async Task Reject(Func<Task> action, string name) { try { await action(); } catch (InvalidOperationException) { Console.WriteLine("OK " + name); return; } throw new Exception(name); }
var filtro = new FiltroPeriodo { Inicio = new(2026, 9, 1), Fim = new(2026, 9, 30), DemandaId = 7, Status = "Finalizada" };
var painel = new Painel { Filtro = filtro, ValorHora = 100, Linhas = [new() { DemandaId = 7, Data = filtro.Inicio, Titulo = "Correção de integração e revisão", Descricao = "Descrição com acentuação: ç, ã, é.", Inicio = filtro.Inicio, Fim = filtro.Inicio.AddHours(2) }] };
var relatorios = new RelatoriosServico(new PainelFalso(painel));
Check(WhatsAppRelatoriosServico.NormalizarTelefone("(11) 99999-9999") == "5511999999999", "Telefone normalizado como no projeto de referência");
await Reject(() => Task.FromResult(WhatsAppRelatoriosServico.NormalizarTelefone("123")), "Rejeita telefone inválido");
var mensagem = WhatsAppRelatoriosServico.Mensagem("Olá {cliente}, {horas}, {valor}, {saldo}: {inicio} a {fim}", "Empresa {saldo}", painel);
Check(mensagem.Contains("Empresa {saldo}") && mensagem.Contains("200,00") && mensagem.Contains("01/09/2026"), "Variáveis substituídas uma vez, preservando nome literal");
await Reject(() => Task.FromResult(WhatsAppRelatoriosServico.Mensagem("{inexistente}", "Teste", painel)), "Rejeita variável inválida");

var pdf = relatorios.GerarPdf(painel, "Usuário de teste");
using (var doc = PdfReader.Open(new MemoryStream(pdf), PdfDocumentOpenMode.Import)) Check(doc.PageCount == 1, "PDF válido com relatório de uma página");
using (var xlsx = new XLWorkbook(new MemoryStream(relatorios.GerarExcel(painel, "Teste"))))
{
    Check(xlsx.Worksheet(1).Cell(7, 5).GetValue<decimal>() == 200, "Excel contém o valor calculado do mesmo relatório");
    Check(xlsx.Worksheets.Count == 2, "Excel mantém aba de pagamentos");
}
var output = Path.Combine(root, "obj", "whatsapp-checks"); Directory.CreateDirectory(output);
await File.WriteAllBytesAsync(Path.Combine(output, "relatorio.pdf"), pdf);
var grande = new Painel { Filtro = filtro, Linhas = Enumerable.Range(1, 60).Select(i => new LinhaRelatorio { DemandaId = i, Data = filtro.Inicio, Titulo = "Demanda " + i, Descricao = new string('W', 240) + "\nDescrição em outra linha.", Inicio = filtro.Inicio, Fim = filtro.Inicio.AddMinutes(30) }).ToList() };
var pdfGrande = relatorios.GerarPdf(grande, "Usuário com relatório extenso");
using (var doc = PdfReader.Open(new MemoryStream(pdfGrande), PdfDocumentOpenMode.Import)) Check(doc.PageCount > 3, "PDF pagina descrições longas e não exige valor/hora");
await File.WriteAllBytesAsync(Path.Combine(output, "relatorio-longo.pdf"), pdfGrande);

var repo = new RepositorioFalso(); var gateway = new GatewayFalso();
var service = new WhatsAppRelatoriosServico(repo, gateway, relatorios, NullLogger<WhatsAppRelatoriosServico>.Instance);
await service.SalvarContatoAsync(10, new() { Nome = "Cliente", Telefone = "11999999999", Template = "Olá {cliente}" });
Check(repo.Contato.Telefone == "5511999999999", "Contato e template passam normalizados à persistência");
await Reject(() => service.EnviarAsync(11, "Outro usuário", new() { ContatoId = 1, Chave = Guid.NewGuid(), Filtro = filtro }), "Não acessa contato de outro usuário");
var pedido = new EnviarRelatorioWhatsApp { ContatoId = 1, Chave = Guid.NewGuid(), Filtro = filtro };
var id = await service.EnviarAsync(10, "Teste", pedido);
await service.EnviarAsync(10, "Teste", pedido);
Check(gateway.Envios == 1 && repo.Itens[id].Status == "Aceito", "Repetir a mesma solicitação não duplica envio");
Check(repo.Itens[id].Arquivo.SequenceEqual(gateway.Ultimo!.Arquivo) && repo.Itens[id].Mensagem == "Olá Cliente", "Histórico preserva exatamente o anexo e a mensagem transmitidos");
Check(repo.Itens[id].DemandaId == 7 && repo.Itens[id].FiltroStatus == "Finalizada", "Histórico preserva filtros da tela Valores");
repo.FalharFinalizacao = true;
var incompleto = new EnviarRelatorioWhatsApp { ContatoId = 1, Chave = Guid.NewGuid(), Filtro = filtro, Tipo = "excel" };
await Reject(() => service.EnviarAsync(10, "Teste", incompleto), "Falha ao registrar resposta não é apresentada como sucesso");
await service.EnviarAsync(10, "Teste", incompleto);
Check(gateway.Envios == 2 && gateway.Ultimo!.Mime.Contains("spreadsheetml"), "Falha ao finalizar histórico não dispara novamente; Excel tem MIME correto");
repo.FalharFinalizacao = false; gateway.Conectado = false;
var falha = await service.EnviarAsync(10, "Teste", new() { ContatoId = 1, Chave = Guid.NewGuid(), Filtro = filtro });
Check(repo.Itens[falha].Status == "Falhou" && gateway.Envios == 2, "Conexão ausente fica no histórico sem disparo");

var configGateway = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Evolution:Url"]="http://evolution.test", ["Evolution:ApiKey"]="chave-simulada" }).Build();
var handler = new HandlerFalso();
var evolution = new EvolutionWhatsAppGateway(new HttpClient(handler), configGateway);
handler.Respostas.Enqueue(new(HttpStatusCode.OK) { Content = new StringContent("[]") });
handler.Respostas.Enqueue(new(HttpStatusCode.Created) { Content = new StringContent("{\"qrcode\":{\"base64\":\"imagem-qr\"}}") });
var conta = await evolution.ConectarAsync("controle-horas-10");
Check(conta.Qrcode == "imagem-qr" && handler.Requisicoes[1].Body.Contains("WHATSAPP-BAILEYS"), "Cria e conecta com o mesmo contrato Evolution e interpreta QR aninhado");
handler.Respostas.Enqueue(new(HttpStatusCode.OK) { Content = new StringContent("[{\"instance\":{\"instanceName\":\"controle-horas-10\",\"status\":\"open\"}}]") });
Check((await evolution.StatusAsync("controle-horas-10")).Conectado, "Interpreta conta conectada no envelope legado");
handler.Respostas.Enqueue(new(HttpStatusCode.OK) { Content = new StringContent("{\"key\":{\"id\":\"abc\"}}") });
var resultado = await evolution.EnviarAsync(repo.Itens[id]);
using (var body = JsonDocument.Parse(handler.Requisicoes.Last().Body))
{
    Check(body.RootElement.GetProperty("mediatype").GetString() == "document" && body.RootElement.GetProperty("mimetype").GetString() == "application/pdf", "Evolution recebe documento PDF e não imagem");
    Check(Convert.FromBase64String(body.RootElement.GetProperty("media").GetString()!).SequenceEqual(repo.Itens[id].Arquivo), "Payload base64 contém o PDF integral");
}
Check(resultado.Status == "Aceito" && resultado.Id == "abc", "Persiste protocolo sem confundir aceitação com entrega");
handler.Respostas.Enqueue(new(HttpStatusCode.BadRequest) { Content = new StringContent("segredo do provedor") });
resultado = await evolution.EnviarAsync(repo.Itens[id]);
Check(resultado.Status == "Falhou" && !resultado.Erro!.Contains("segredo"), "Falha 400 não expõe resposta privada");
handler.Falhar = true;
var chamadas = handler.Requisicoes.Count;
resultado = await evolution.EnviarAsync(repo.Itens[id]);
Check(resultado.Status == "Incerto" && handler.Requisicoes.Count == chamadas + 1, "Falha de rede não causa repetição automática");
Console.WriteLine("Todos os testes simulados passaram. Nenhuma mensagem real foi enviada.");

sealed class PainelFalso(Painel painel) : IPainelServico { public Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null) => Task.FromResult(painel); }
sealed class GatewayFalso : IWhatsAppGateway
{
    public bool Configurado => true; public string Prefixo => "teste"; public bool Conectado = true;
    public int Envios; public EnvioRelatorioWhatsApp? Ultimo;
    public Task<ContaWhatsApp> StatusAsync(string i) => Task.FromResult(new ContaWhatsApp(i, Conectado));
    public Task<ContaWhatsApp> ConectarAsync(string i) => StatusAsync(i);
    public Task DesconectarAsync(string i) => Task.CompletedTask;
    public Task<ResultadoWhatsApp> EnviarAsync(EnvioRelatorioWhatsApp e) { Envios++; Ultimo = e; return Task.FromResult(new ResultadoWhatsApp("Aceito", "teste")); }
}
sealed class RepositorioFalso : IWhatsAppRepositorio
{
    public ContatoWhatsApp Contato = new() { Id = 1, Nome = "Cliente", Telefone = "5511999999999" };
    public Dictionary<long,EnvioRelatorioWhatsApp> Itens = []; public bool FalharFinalizacao;
    public Task<List<ContatoWhatsApp>> ContatosAsync(int u) => Task.FromResult(new List<ContatoWhatsApp> { Contato });
    public Task<ContatoWhatsApp?> ContatoAsync(int u,int id) => Task.FromResult(u == 10 && id == 1 ? Contato : null);
    public Task SalvarContatoAsync(int u,ContatoWhatsApp c) { c.Id = 1; Contato = c; return Task.CompletedTask; }
    public Task<string> InstanciaAsync(int u,string p) => Task.FromResult($"{p}-{u}");
    public Task<(EnvioRelatorioWhatsApp Envio,bool Novo)> ReservarAsync(EnvioRelatorioWhatsApp e) { var anterior = Itens.Values.FirstOrDefault(x=>x.UsuarioId==e.UsuarioId && x.Chave==e.Chave); if(anterior != null) return Task.FromResult((anterior,false)); e.Id=Itens.Count+1; Itens[e.Id]=e; return Task.FromResult((e,true)); }
    public Task FinalizarAsync(int u,long id,ResultadoWhatsApp r) { if(FalharFinalizacao) throw new InvalidOperationException("falha simulada"); Itens[id].Status=r.Status; return Task.CompletedTask; }
    public Task<EnvioRelatorioWhatsApp?> ObterEnvioAsync(int u,long id,bool incluirArquivo=false) => Task.FromResult(Itens.TryGetValue(id,out var e) && e.UsuarioId==u ? e : null);
    public Task<(List<EnvioRelatorioWhatsApp> Itens,int Total)> HistoricoAsync(int u,int p) => Task.FromResult((Itens.Values.Where(x=>x.UsuarioId==u).ToList(),Itens.Count));
}
sealed class HandlerFalso : HttpMessageHandler
{
    public Queue<HttpResponseMessage> Respostas = []; public List<(string Url,string Body)> Requisicoes = []; public bool Falhar;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct)
    {
        Requisicoes.Add((r.RequestUri!.AbsolutePath,r.Content is null ? "" : await r.Content.ReadAsStringAsync(ct)));
        if(Falhar) throw new HttpRequestException("rede indisponível");
        return Respostas.Dequeue();
    }
}
