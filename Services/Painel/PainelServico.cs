using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using static ControleHoras.Services.Horario;
using static ControleHoras.Services.Validacao;
using static ControleHoras.Services.CalculoHoras;

namespace ControleHoras.Services;

public class PainelServico(IDemandasRepositorio demandas, IApontamentosRepositorio apontamentosRepositorio, IConfiguracoesRepositorio configuracoes, IPagamentosRepositorio pagamentosRepositorio) : IPainelServico
{
    public async Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null)
    {
        filtro ??= new() { Inicio = new(AgoraLocal.Year, AgoraLocal.Month, 1), Fim = AgoraLocal.Date };
        Exigir(filtro.Inicio != default && filtro.Fim >= filtro.Inicio && (filtro.Fim - filtro.Inicio).TotalDays <= 3660, "Informe um período válido de até 10 anos.");
        var apontamentos = await apontamentosRepositorio.ApontamentosAsync(usuarioId);
        var pagamentos = await pagamentosRepositorio.PagamentosAsync(usuarioId);
        var painel = new Painel { Filtro = filtro, Demandas = await demandas.DemandasAsync(usuarioId), Projetos = await demandas.ProjetosAsync(usuarioId), ValorHora = await configuracoes.ValorHoraAsync(usuarioId) };
        painel.Linhas = Recortar(apontamentos.Where(a => (!filtro.DemandaId.HasValue || a.DemandaId == filtro.DemandaId) && (string.IsNullOrEmpty(filtro.Status) || a.Status == filtro.Status)), filtro.Inicio, filtro.Fim);
        // Pagamentos são atribuídos integralmente à competência informada, nunca rateados silenciosamente.
        painel.Pagamentos = pagamentos.Where(p => p.PeriodoInicio.Date >= filtro.Inicio.Date && p.PeriodoFim.Date <= filtro.Fim.Date).ToList();
        painel.SegundosHoje = Recortar(apontamentos, AgoraLocal.Date, AgoraLocal.Date).Sum(l => l.Segundos);
        painel.SegundosMes = Recortar(apontamentos, new(AgoraLocal.Year, AgoraLocal.Month, 1), AgoraLocal.Date).Sum(l => l.Segundos);
        painel.SegundosTotais = apontamentos.Sum(a => ((a.Fim ?? DateTime.UtcNow) - a.Inicio).TotalSeconds);
        painel.SaldoGeral = painel.ValorHora.HasValue ? (decimal)painel.SegundosTotais / 3600m * painel.ValorHora.Value - pagamentos.Sum(p => p.ValorPago) : null;
        return painel;
    }
}
