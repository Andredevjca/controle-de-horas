using ControleHoras.Interfaces.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace ControleHoras.Controllers;
public class ConfiguracoesController(IConfiguracoesServico servico) : ControladorBase
{
    public async Task<IActionResult> Index() { ViewBag.ValorHora = await servico.ValorHoraAsync(UsuarioId); return View(); }
    [HttpPost] public Task<IActionResult> Salvar(decimal? valorHora) => ExecutarAsync(() => servico.DefinirValorAsync(UsuarioId, valorHora), "/Configuracoes");
    [HttpPost] public Task<IActionResult> Senha(string senhaAtual, string novaSenha, string confirmarSenha) => ExecutarAsync(async () => {
        await servico.AlterarSenhaAsync(UsuarioId, senhaAtual, novaSenha, confirmarSenha);
        await HttpContext.SignOutAsync();
    }, "/Configuracoes");
}
