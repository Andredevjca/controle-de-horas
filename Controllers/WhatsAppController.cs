using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using ControleHoras.Models;
using ControleHoras.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleHoras.Controllers;

public sealed class WhatsAppController(WhatsAppRelatoriosServico servico, IWhatsAppRepositorio repositorio,
    IRelatoriosServico relatorios, ILogger<WhatsAppController> logger) : ControladorBase
{
    private async Task<WhatsAppPagina> PaginaAsync(FiltroPeriodo filtro, int pagina = 1, ContatoWhatsApp? edicao = null)
    {
        pagina = Math.Clamp(pagina, 1, 100000);
        var resumo = await relatorios.PainelAsync(UsuarioId, filtro);
        var historico = await repositorio.HistoricoAsync(UsuarioId, pagina);
        return new() { Filtro = filtro, Resumo = resumo, Contatos = await repositorio.ContatosAsync(UsuarioId),
            Historico = historico.Itens, Total = historico.Total, Pagina = pagina, Edicao = edicao ?? new(), Configurado = servico.Configurado };
    }
    [HttpGet]
    public async Task<IActionResult> Index(FiltroPeriodo filtro, int pagina = 1, int? editar = null)
    {
        if (!ModelState.IsValid) return BadRequest("Filtros inválidos.");
        var contato = editar.HasValue ? await repositorio.ContatoAsync(UsuarioId, editar.Value) : null;
        if (editar.HasValue && contato is null) return NotFound();
        try { return View(await PaginaAsync(filtro, pagina, contato)); }
        catch (InvalidOperationException e) { TempData["Erro"] = e.Message; return RedirectToAction("Valores", "Relatorios"); }
    }
    [HttpPost]
    public async Task<IActionResult> SalvarContato(ContatoWhatsApp contato, FiltroPeriodo filtro)
    {
        if (ModelState.IsValid)
        {
            try { await servico.SalvarContatoAsync(UsuarioId, contato); TempData["Sucesso"] = "Cliente e template salvos."; return Voltar(filtro); }
            catch (InvalidOperationException e) { ModelState.AddModelError("", e.Message); }
        }
        // Keep what the user typed when validation fails.
        try { return View("Index", await PaginaAsync(filtro, edicao: contato)); }
        catch (InvalidOperationException e) { TempData["Erro"] = e.Message; return RedirectToAction("Valores", "Relatorios"); }
    }
    private IActionResult Voltar(FiltroPeriodo f) => RedirectToAction(nameof(Index), new { f.Inicio, f.Fim, f.DemandaId, f.Status });
    [HttpPost]
    public async Task<IActionResult> Enviar(EnviarRelatorioWhatsApp pedido)
    {
        if (!ModelState.IsValid) { TempData["Erro"] = "Confira o cliente, o formato e o período do relatório."; return Voltar(pedido.Filtro); }
        try
        {
            var id = await servico.EnviarAsync(UsuarioId, User.Identity?.Name, pedido);
            return RedirectToAction(nameof(Detalhes), new { id });
        }
        catch (InvalidOperationException e) { TempData["Erro"] = e.Message; return Voltar(pedido.Filtro); }
        catch (Exception e)
        {
            logger.LogError(e, "Não foi possível concluir a solicitação de envio WhatsApp do usuário {UsuarioId}", UsuarioId);
            TempData["Erro"] = "Não foi possível concluir a solicitação. Consulte o histórico antes de tentar novamente.";
            return Voltar(pedido.Filtro);
        }
    }
    [HttpGet]
    public async Task<IActionResult> Detalhes(long id)
    {
        var envio = await repositorio.ObterEnvioAsync(UsuarioId, id);
        return envio is null ? NotFound() : View(envio);
    }
    [HttpGet]
    public async Task<IActionResult> Arquivo(long id)
    {
        var envio = await repositorio.ObterEnvioAsync(UsuarioId, id, true);
        if (envio is null) return NotFound();
        Response.Headers.CacheControl = "no-store";
        return File(envio.Arquivo, envio.Mime, envio.NomeArquivo);
    }
    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<IActionResult> Status() => ConexaoAsync(() => servico.StatusAsync(UsuarioId));
    [HttpPost]
    public Task<IActionResult> Conectar() => ConexaoAsync(() => servico.ConectarAsync(UsuarioId));
    [HttpPost]
    public async Task<IActionResult> Desconectar()
    {
        try { await servico.DesconectarAsync(UsuarioId); return Json(new { conectado = false }); }
        catch (Exception e) { logger.LogWarning(e, "Falha ao desconectar WhatsApp"); return StatusCode(502, new { erro = "Não foi possível desconectar. Atualize o status." }); }
    }
    private async Task<IActionResult> ConexaoAsync(Func<Task<ContaWhatsApp>> operacao)
    {
        try { return Json(await operacao()); }
        catch (InvalidOperationException e) { return StatusCode(502, new { erro = e.Message }); }
        catch (Exception e) { logger.LogWarning(e, "Falha na conexão WhatsApp"); return StatusCode(502, new { erro = "Não foi possível consultar a Evolution. Verifique a conexão e tente novamente." }); }
    }
}
