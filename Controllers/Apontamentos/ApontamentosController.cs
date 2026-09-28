using ControleHoras.Models;
using ControleHoras.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleHoras.Controllers;
public class ApontamentosController(IApontamentosServico servico) : ControladorBase
{
    public async Task<IActionResult> Index(FiltroPeriodo filtro)
    {
        if (!ModelState.IsValid) { TempData["Erro"] = "Informe datas válidas."; return RedirectToAction(nameof(Index)); }
        try { return View("Index", await servico.PainelAsync(UsuarioId, filtro)); }
        catch (InvalidOperationException erro) { TempData["Erro"] = erro.Message; return RedirectToAction(nameof(Index)); }
    }
    public async Task<IActionResult> Manual() { ViewBag.Demandas = await servico.DemandasAsync(UsuarioId); return View(new LancamentoManual()); }
    [HttpPost] public Task<IActionResult> Salvar(LancamentoManual lancamento) => ExecutarAsync(() => servico.LancarAsync(UsuarioId, lancamento), "/Apontamentos");
    public async Task<IActionResult> Historico(FiltroPeriodo filtro)
    {
        if (!ModelState.IsValid) { TempData["Erro"] = "Informe datas válidas."; return RedirectToAction(nameof(Historico)); }
        try
        {
            ViewBag.Historico = await servico.HistoricoAsync(UsuarioId);
            return View(await servico.PainelAsync(UsuarioId, filtro));
        }
        catch (InvalidOperationException erro) { TempData["Erro"] = erro.Message; return RedirectToAction(nameof(Historico)); }
    }
}
