using System.Security.Claims;
using System.Text.Encodings.Web;
using ControleHoras.Controllers;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using ControleHoras.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

static class ScreenChecks
{
    public static async Task RunAsync(IPainelServico painel, IPagamentosServico pagamentos, IPagamentosRepositorio repo, IDemandasRepositorio demandas, IApontamentosRepositorio apontamentos, IHistoricoRepositorio historico, IUnidadeTrabalho unidade, DateTime inicio, DateTime fim)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = Directory.GetCurrentDirectory() });
        builder.WebHost.UseUrls("http://127.0.0.1:5298");
        builder.Logging.ClearProviders();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute())).AddApplicationPart(typeof(PagamentosController).Assembly);
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(painel); builder.Services.AddSingleton(pagamentos); builder.Services.AddSingleton(repo);
        builder.Services.AddSingleton(demandas); builder.Services.AddSingleton(apontamentos); builder.Services.AddSingleton(historico); builder.Services.AddSingleton(unidade);
        builder.Services.AddScoped<IRelatoriosServico, RelatoriosServico>();
        builder.Services.AddScoped<IDemandasServico, DemandasServico>(); builder.Services.AddScoped<IApontamentosServico, ApontamentosServico>();
        await using var app = builder.Build();
        app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
        app.MapControllerRoute("default", "{controller=Pagamentos}/{action=Index}/{id?}");
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5298") };
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("OK: " + message); }
        async Task<string> Get(string path) { var response = await client.GetAsync(path); Check(response.IsSuccessStatusCode, "HTTP " + path); return System.Net.WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()); }
        try
        {
            var query = $"?Inicio={inicio:yyyy-MM-dd}&Fim={fim:yyyy-MM-dd}";
            var pagamento = await Get("/Pagamentos" + query);
            Check(!pagamento.Contains("Demanda A") && pagamento.Contains("Demanda B") && !pagamento.Contains("Recebimentos registrados"), "Pagamentos renderiza só pendentes e não lista recibos");
            var demanda = await Get("/Demandas");
            Check(!demanda.Contains("Demanda A") && demanda.Contains("Demanda B"), "Demandas oculta quitadas");
            var horas = await Get("/Apontamentos" + query);
            Check(!horas.Contains("Demanda A") && horas.Contains("Demanda B"), "Apontamentos oculta quitadas");
            var historicoHtml = await Get("/Apontamentos/Historico" + query);
            Check(historicoHtml.Contains("Demanda A") && historicoHtml.Contains("Demanda B") && historicoHtml.Contains(">Pago</span>") && historicoHtml.Contains(">Não pago</span>"), "Histórico exibe pagos e não pagos com status");
            var valores = await Get("/Relatorios/Valores" + query);
            Check(!valores.Contains(">Demanda A</a>") && valores.Contains("Demanda B") && valores.Contains("Pagamento=Pendente"), "Valores e exportações mantêm filtro de pendências");
            var relatorio = await Get("/Relatorios" + query + "&Pagamento=Pago");
            Check(relatorio.Contains(">Demanda A</a>") && !relatorio.Contains(">Demanda B</a>"), "Relatório filtra status financeiro");
            var financeiro = await Get("/Financeiro?Inicio=2026-09-01&Fim=2026-10-31");
            Check(financeiro.Contains("09/2026") && financeiro.Contains("10/2026") && financeiro.Contains("Demanda A") && financeiro.Contains("01/10/2026"), "Financeiro mostra meses e baixa na data do recebimento");
            Check(financeiro.Contains("Evolução por mês") && financeiro.Contains("progressbar") && !financeiro.Contains("<th>Situação</th>") && !financeiro.Contains("Pago pelas horas"), "Resumo mensal mostra barras sem coluna de situação");
            var forbidden = await client.PostAsync("/Pagamentos/Baixar", new FormUrlEncodedContent(new Dictionary<string,string> { ["Inicio"] = "2026-09-01", ["Fim"] = "2026-09-30", ["DemandaIds"] = "2" }));
            Check(forbidden.StatusCode == System.Net.HttpStatusCode.BadRequest, "Baixa exige token antifalsificação");
        }
        finally { await app.StopAsync(); }
    }
    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.Success(new(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "7"), new Claim(ClaimTypes.Name, "Teste financeiro") }, Scheme.Name)), Scheme.Name)));
    }
}
