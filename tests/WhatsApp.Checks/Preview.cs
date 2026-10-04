using System.Security.Claims;
using System.Text.Encodings.Web;
using ControleHoras.Controllers;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using ControleHoras.Models;
using ControleHoras.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Runs only in this test executable with explicit --preview. No real database or Evolution access.
static class Preview
{
    public static async Task RunAsync(string root)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = root, WebRootPath = Path.Combine(root, "wwwroot") });
        builder.WebHost.UseUrls("http://127.0.0.1:5199");
        builder.Logging.ClearProviders(); builder.Logging.AddConsole();
        builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(root, "obj", "preview-keys"))).UseEphemeralDataProtectionProvider();
        builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
            .AddApplicationPart(typeof(WhatsAppController).Assembly);
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IWhatsAppRepositorio>(new RepositorioFalso());
        builder.Services.AddSingleton<IWhatsAppGateway>(new GatewayFalso());
        var filtro = new FiltroPeriodo { Inicio = new(2026, 9, 1), Fim = new(2026, 9, 30) };
        builder.Services.AddSingleton<IPainelServico>(new PainelFalso(new Painel { Filtro = filtro, ValorHora = 120, Linhas = [new() { Data = filtro.Inicio, DemandaId = 7, Titulo = "Integração de relatórios", Inicio = filtro.Inicio, Fim = filtro.Inicio.AddHours(2) }] }));
        builder.Services.AddScoped<IRelatoriosServico, RelatoriosServico>();
        builder.Services.AddScoped<WhatsAppRelatoriosServico>();
        var app = builder.Build();
        app.UseStaticFiles(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
        app.MapControllerRoute("default", "{controller=WhatsApp}/{action=Index}/{id?}");
        await app.RunAsync();
    }
    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "10"), new Claim(ClaimTypes.Name, "Teste de interface") };
            return Task.FromResult(AuthenticateResult.Success(new(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }
}
