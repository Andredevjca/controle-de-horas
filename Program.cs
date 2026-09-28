using System.Globalization;
using ControleHoras.Data;
using ControleHoras.Models;
using ControleHoras.Interfaces.Services;
using ControleHoras.Dependecias;
using ControleHoras.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

var construtor = WebApplication.CreateBuilder(args);
construtor.Configuration.AddJsonFile("appsettings.Local.json", optional: true).AddEnvironmentVariables();
construtor.Services.AdicionarDependencias();
construtor.Services.AddControllersWithViews(opcoes => opcoes.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
construtor.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(opcoes => {
    opcoes.LoginPath = "/Autenticacao/Entrar";
    opcoes.AccessDeniedPath = "/Autenticacao/Negado";
    opcoes.ExpireTimeSpan = TimeSpan.FromHours(8);
    opcoes.Cookie.HttpOnly = true;
    opcoes.Cookie.SameSite = SameSiteMode.Lax;
    opcoes.Events.OnValidatePrincipal = async contexto => {
        var repositorio = contexto.HttpContext.RequestServices.GetRequiredService<IAutenticacaoServico>();
        var identificador = contexto.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var usuario = int.TryParse(identificador, out var id) ? await repositorio.ObterUsuarioAsync(id) : null;
        if (usuario is null || !usuario.Ativo || usuario.VersaoSessao.ToString() != contexto.Principal?.FindFirst("versao")?.Value)
            contexto.RejectPrincipal();
    };
});
construtor.Services.AddAuthorization();
construtor.Services.AddRateLimiter(opcoes => {
    opcoes.RejectionStatusCode = 429;
    opcoes.AddPolicy("login", contexto => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions {
            PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
});
var aplicacao = construtor.Build();
var cultura = new CultureInfo("pt-BR");
aplicacao.UseRequestLocalization(new RequestLocalizationOptions().SetDefaultCulture("pt-BR").AddSupportedCultures("pt-BR").AddSupportedUICultures("pt-BR"));
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;
if (!aplicacao.Environment.IsDevelopment()) { aplicacao.UseExceptionHandler("/Autenticacao/Erro"); aplicacao.UseHsts(); }
aplicacao.UseStaticFiles();
aplicacao.UseRouting();
aplicacao.UseRateLimiter();
aplicacao.UseAuthentication();
aplicacao.UseAuthorization();
await aplicacao.Services.GetRequiredService<BancoDados>().InicializarAsync();
aplicacao.MapControllerRoute("padrao", "{controller=Dashboard}/{action=Index}/{id?}");
aplicacao.Run();


