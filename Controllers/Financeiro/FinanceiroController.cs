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
                Demandas = horas.Linhas.Select(l => l.DemandaId).Distinct().Count(), Saldo = horas.Saldo,
                TotalRecebidoGeral = todos.Sum(p => p.ValorPago) };
            for (var mes = new DateTime(filtro.Inicio.Year, filtro.Inicio.Month, 1); mes <= filtro.Fim; mes = mes.AddMonths(1))
            {
                var linhas = horas.Linhas.Where(l => l.Data.Year == mes.Year && l.Data.Month == mes.Month).ToList();
                var pagos = recebimentos.Where(p => p.DataPagamento.Year == mes.Year && p.DataPagamento.Month == mes.Month).ToList();
                modelo.Meses.Add(new FinanceiroMes { Mes = mes,
                    Demandas = linhas.Select(l => l.DemandaId).Distinct().Count(),
                    DemandasRecebidas = pagos.Where(p => p.DemandaId.HasValue).Select(p => p.DemandaId).Distinct().Count(),
                    Segundos = linhas.Sum(l => l.Segundos), Estimado = horas.ValorHora.HasValue ? linhas.Sum(l => l.Estimado ?? 0) : null,
                    Recebido = pagos.Sum(p => p.ValorPago), Saldo = horas.ValorHora.HasValue ? linhas.Sum(l => l.Saldo ?? 0) : null });
            }
            return View(modelo);
        }
        catch (InvalidOperationException erro) { TempData["Erro"] = erro.Message; return RedirectToAction(nameof(Index)); }
    }
}
