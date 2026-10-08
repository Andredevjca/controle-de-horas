using ControleHoras.Models;
using ControleHoras.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleHoras.Controllers;
public class PagamentosController(IPagamentosServico servico) : ControladorBase
{
    public async Task<IActionResult> Index(FiltroPeriodo filtro)
    {
        if (!ModelState.IsValid) { TempData["Erro"] = "Informe datas válidas."; return RedirectToAction(nameof(Index)); }
        try { return View("Index", await servico.PainelAsync(UsuarioId, filtro)); }
        catch (InvalidOperationException erro) { TempData["Erro"] = erro.Message; return RedirectToAction(nameof(Index)); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Baixar(BaixaPagamentos baixa) => ExecutarAsync(
        () => servico.BaixarAsync(UsuarioId, baixa),
        Url.Action(nameof(Index), new { Inicio = baixa.Inicio.ToString("yyyy-MM-dd"), Fim = baixa.Fim.ToString("yyyy-MM-dd") })!);

    [HttpPost] public Task<IActionResult> Salvar(Pagamento pagamento) => ExecutarAsync(() => servico.PagarAsync(UsuarioId, pagamento), "/Pagamentos");
}
