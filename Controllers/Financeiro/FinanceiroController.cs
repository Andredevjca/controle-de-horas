using ControleHoras.Models;
using ControleHoras.Interfaces.Services;
using ControleHoras.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ControleHoras.Controllers;
public class FinanceiroController(IPainelServico painel, IPagamentosRepositorio pagamentos) : ControladorBase
{
    public async Task<IActionResult> Index(FiltroPeriodo filtro)
    {
        if (!ModelState.IsValid) { TempData["Erro"] = "Informe datas válidas."; return RedirectToAction(nameof(Index)); }
        try
        {
            var horas = await painel.PainelAsync(UsuarioId, filtro);
            var todos = await pagamentos.PagamentosAsync(UsuarioId);
            var recebimentos = todos.Where(p => p.DataPagamento.Date >= filtro.Inicio.Date && p.DataPagamento.Date <= filtro.Fim.Date).ToList();
            var modelo = new FinanceiroPainel { Filtro = filtro, Recebimentos = recebimentos,
                Segundos = horas.Segundos, Demandas = horas.Linhas.Select(l => l.DemandaId).Distinct().Count(), Saldo = horas.Saldo,
                TotalRecebidoGeral = todos.Sum(p => p.ValorPago) };
            for (var mes = new DateTime(filtro.Inicio.Year, filtro.Inicio.Month, 1); mes <= filtro.Fim; mes = mes.AddMonths(1))
            {
                var baixas = recebimentos.Where(p => p.DataPagamento.Year == mes.Year && p.DataPagamento.Month == mes.Month).ToList();
                modelo.Meses.Add(new FinanceiroMes { Mes = mes,
                    DemandasRecebidas = baixas.Where(p => p.DemandaId.HasValue).Select(p => p.DemandaId).Distinct().Count(),
                    QuantidadeBaixas = baixas.Count,
                    Recebido = baixas.Sum(p => p.ValorPago) });
            }
            return View(modelo);
        }
        catch (InvalidOperationException erro) { TempData["Erro"] = erro.Message; return RedirectToAction(nameof(Index)); }
    }
}
