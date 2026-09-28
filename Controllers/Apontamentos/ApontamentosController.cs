using System.Security.Claims;
using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using ControleHoras.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ClosedXML.Excel;

namespace ControleHoras.Controllers;
public class ApontamentosController(IControleServico servico, IControleRepositorio repositorio) : ControladorBase
{
    public async Task<IActionResult> Index(FiltroPeriodo filtro)
    {
        if (!ModelState.IsValid) { TempData["Erro"] = "Informe datas válidas."; return RedirectToAction(nameof(Index)); }
        try { return View("Index", await servico.PainelAsync(UsuarioId, filtro)); }
        catch (InvalidOperationException erro) { TempData["Erro"] = erro.Message; return RedirectToAction(nameof(Index)); }
    }
    public async Task<IActionResult> Manual() { ViewBag.Demandas = await repositorio.DemandasAsync(UsuarioId); return View(new LancamentoManual()); }
    [HttpPost] public Task<IActionResult> Salvar(LancamentoManual lancamento) => ExecutarAsync(() => servico.LancarAsync(UsuarioId, lancamento), "/Apontamentos");
    public async Task<IActionResult> Historico(FiltroPeriodo filtro)
    {
        if (!ModelState.IsValid) { TempData["Erro"] = "Informe datas válidas."; return RedirectToAction(nameof(Historico)); }
        try
        {
            ViewBag.Historico = await repositorio.HistoricoAsync(UsuarioId);
            return View(await servico.PainelAsync(UsuarioId, filtro));
        }
        catch (InvalidOperationException erro) { TempData["Erro"] = erro.Message; return RedirectToAction(nameof(Historico)); }
    }
}


