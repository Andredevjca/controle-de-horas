using ControleHoras.Models;
using ControleHoras.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleHoras.Controllers;
public class DemandasController(IDemandasServico servico) : ControladorBase
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
        ViewBag.Projetos = await servico.ProjetosAsync(UsuarioId);
        var demanda = id == 0 ? new Demanda() : (await servico.DemandasAsync(UsuarioId)).FirstOrDefault(d => d.Id == id);
        return demanda == null ? NotFound() : View(demanda);
    }
    [HttpPost] public Task<IActionResult> Salvar(Demanda demanda) => ExecutarAsync(() => servico.SalvarDemandaAsync(UsuarioId, demanda), "/Demandas");
    public async Task<IActionResult> Detalhes(int id)
    {
        var demanda = await servico.DetalhesDemandaAsync(UsuarioId, id);
        if (demanda == null) return NotFound();
        ViewBag.Historico = (await servico.HistoricoAsync(UsuarioId)).Where(h => h.DemandaId == id).ToList();
        return View(demanda);
    }
    [HttpPost] public Task<IActionResult> Acao(int id, string acao, string? retorno) => ExecutarAsync(() => servico.MudarTrabalhoAsync(UsuarioId, id, acao), Url.IsLocalUrl(retorno) ? retorno! : "/Dashboard");
    [HttpPost] public Task<IActionResult> Projeto(string nome) => ExecutarAsync(() => servico.CriarProjetoAsync(UsuarioId, nome), "/Demandas/Editar");
}
