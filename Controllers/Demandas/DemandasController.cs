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
public class DemandasController(IControleServico servico, IControleRepositorio repositorio) : ControladorBase
{
    public async Task<IActionResult> Index(string? busca, string? status)
    {
        var painel = await servico.PainelAsync(UsuarioId);
        painel.Demandas = painel.Demandas.Where(d => (string.IsNullOrWhiteSpace(busca) || d.Titulo.Contains(busca, StringComparison.OrdinalIgnoreCase) || d.Codigo.Contains(busca, StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrEmpty(status) || d.Status == status)).ToList();
        ViewBag.Busca = busca; ViewBag.Status = status;
        return View(painel);
    }
    public async Task<IActionResult> Editar(int id = 0)
    {
        ViewBag.Projetos = await repositorio.ProjetosAsync(UsuarioId);
        var demanda = id == 0 ? new Demanda() : (await repositorio.DemandasAsync(UsuarioId)).FirstOrDefault(d => d.Id == id);
        return demanda == null ? NotFound() : View(demanda);
    }
    [HttpPost] public Task<IActionResult> Salvar(Demanda demanda) => ExecutarAsync(() => servico.SalvarDemandaAsync(UsuarioId, demanda), "/Demandas");
    public async Task<IActionResult> Detalhes(int id)
    {
        var demanda = await servico.DetalhesDemandaAsync(UsuarioId, id);
        if (demanda == null) return NotFound();
        ViewBag.Historico = (await repositorio.HistoricoAsync(UsuarioId)).Where(h => h.DemandaId == id).ToList();
        return View(demanda);
    }
    [HttpPost] public Task<IActionResult> Acao(int id, string acao, string? retorno) => ExecutarAsync(() => servico.MudarTrabalhoAsync(UsuarioId, id, acao), Url.IsLocalUrl(retorno) ? retorno! : "/Dashboard");
    [HttpPost] public Task<IActionResult> Projeto(string nome) => ExecutarAsync(async () => {
        ControleServico.Exigir(!string.IsNullOrWhiteSpace(nome) && nome.Trim().Length <= 120, "Informe um nome de projeto de até 120 caracteres.");
        await repositorio.CriarProjetoAsync(UsuarioId, nome.Trim());
    }, "/Demandas/Editar");
}

