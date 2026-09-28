using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using static ControleHoras.Services.Horario;
using static ControleHoras.Services.Validacao;

namespace ControleHoras.Services;

public class ApontamentosServico(IDemandasRepositorio demandas, IApontamentosRepositorio apontamentosRepositorio, IHistoricoRepositorio historico, IUnidadeTrabalho unidadeTrabalho, IPainelServico painel) : IApontamentosServico
{
    public Task LancarAsync(int usuarioId, LancamentoManual lancamento) => unidadeTrabalho.TransacionarAsync(usuarioId, async () => {
        Exigir((await demandas.DemandasAsync(usuarioId)).Any(d => d.Id == lancamento.DemandaId), "Selecione uma demanda válida.");
        Exigir(lancamento.Data != default && lancamento.Inicio >= TimeSpan.Zero && lancamento.Inicio < TimeSpan.FromDays(1) && lancamento.Fim >= TimeSpan.Zero && lancamento.Fim < TimeSpan.FromDays(1), "Informe data e horários válidos.");
        Exigir(lancamento.Inicio != lancamento.Fim, "Início e fim devem ser diferentes.");
        var inicio = Utc(lancamento.Data.Date + lancamento.Inicio);
        var fim = Utc(lancamento.Data.Date.AddDays(lancamento.Fim < lancamento.Inicio ? 1 : 0) + lancamento.Fim);
        Exigir(fim <= DateTime.UtcNow, "Não é possível registrar horas futuras.");
        Exigir(!(await apontamentosRepositorio.ApontamentosAsync(usuarioId)).Any(a => inicio < (a.Fim ?? DateTime.MaxValue) && fim > a.Inicio), "O período se sobrepõe a outro apontamento.");
        await apontamentosRepositorio.InserirApontamentoAsync(usuarioId, lancamento.DemandaId, inicio, fim, lancamento.Descricao, true);
        await historico.AuditarAsync(usuarioId, lancamento.DemandaId, "Apontamento manual", $"{lancamento.Data:dd/MM/yyyy} {lancamento.Inicio} até {lancamento.Fim}");
    });
    public Task<List<Demanda>> DemandasAsync(int usuarioId) => demandas.DemandasAsync(usuarioId);
    public Task<List<RegistroHistorico>> HistoricoAsync(int usuarioId) => historico.HistoricoAsync(usuarioId);
    public Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null) => painel.PainelAsync(usuarioId, filtro);
}
