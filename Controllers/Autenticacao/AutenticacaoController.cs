using System.Security.Claims;
using ControleHoras.Interfaces.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ControleHoras.Controllers;
public class AutenticacaoController(IAutenticacaoServico servico) : Controller
{
    [HttpGet] public IActionResult Entrar() => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Dashboard") : View();
    [HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Entrar(string? usuario, string? senha)
    {
        var cadastro = await servico.AutenticarAsync(usuario, senha);
        if (cadastro == null)
        { ModelState.AddModelError("", "Usuário ou senha inválidos."); return View(); }
        var atributos = new List<Claim> { new(ClaimTypes.NameIdentifier, cadastro.Id.ToString()), new(ClaimTypes.Name, cadastro.Nome), new("versao", cadastro.VersaoSessao.ToString()), new(ClaimTypes.Role, cadastro.Administrador ? "Administrador" : "Usuario") };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(atributos, CookieAuthenticationDefaults.AuthenticationScheme)));
        return RedirectToAction("Index", "Dashboard");
    }
    [HttpPost, Authorize] public async Task<IActionResult> Sair() { await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Entrar)); }
    public IActionResult Negado() => StatusCode(403, "Você não tem permissão para acessar esta página.");
    public IActionResult Erro() => Problem("Não foi possível concluir a operação. Tente novamente ou consulte os registros do servidor.");
}
