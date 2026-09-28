using ControleHoras.Models;
using ControleHoras.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleHoras.Controllers;
public class RelatoriosController(IRelatoriosServico servico) : ControladorBase
{
    public async Task<IActionResult> Index(FiltroPeriodo filtro)
    {
        if (!ModelState.IsValid) { TempData["Erro"] = "Informe datas válidas."; return RedirectToAction(nameof(Index)); }
        try { return View("Index", await servico.PainelAsync(UsuarioId, filtro)); }
        catch (InvalidOperationException erro) { TempData["Erro"] = erro.Message; return RedirectToAction(nameof(Index)); }
    }
    public Task<IActionResult> Valores(FiltroPeriodo filtro) => Index(filtro);
    public async Task<IActionResult> Imprimir(FiltroPeriodo filtro)
    {
        if (!ModelState.IsValid) return BadRequest("Período inválido.");
        try { return View("Imprimir", await servico.PainelAsync(UsuarioId, filtro)); }
        catch (InvalidOperationException erro) { return BadRequest(erro.Message); }
    }
    public async Task<IActionResult> Excel(FiltroPeriodo filtro)
    {
        if (!ModelState.IsValid) return BadRequest("Período inválido.");
        try
        {
            var arquivo = await servico.ExportarExcelAsync(UsuarioId, filtro, User.Identity!.Name);
            return File(arquivo, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"horas-{filtro.Inicio:yyyyMMdd}-{filtro.Fim:yyyyMMdd}.xlsx");
        }
        catch (InvalidOperationException erro) { return BadRequest(erro.Message); }
    }
}
