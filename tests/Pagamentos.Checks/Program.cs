using System.Reflection;
using ClosedXML.Excel;
using ControleHoras.Models;
using ControleHoras.Services;
using ControleHoras.Interfaces.Services;
using ControleHoras.Interfaces.Repositories;

var registros = new List<Pagamento>();
var auditorias = 0;
var transacoes = 0;
var falharAuditoria = false;
decimal? tarifa = 100;
var inicio = new DateTime(2026, 9, 1);
var fim = new DateTime(2026, 9, 30);
var horas = new List<Apontamento> {
    new() { DemandaId = 1, Titulo = "Demanda A", Inicio = Horario.Utc(inicio.AddHours(8)), Fim = Horario.Utc(inicio.AddHours(9)) },
    new() { DemandaId = 1, Titulo = "Demanda A", Inicio = Horario.Utc(inicio.AddDays(1).AddHours(8)), Fim = Horario.Utc(inicio.AddDays(1).AddHours(9)) },
    new() { DemandaId = 2, Titulo = "Demanda B", Inicio = Horario.Utc(inicio.AddHours(10)), Fim = Horario.Utc(inicio.AddHours(11)) }
};
var repo = Stub.Make<IPagamentosRepositorio>((name, args) => name switch {
    "PagamentosAsync" => Task.FromResult(registros.ToList()),
    "InserirPagamentoAsync" => Insert((Pagamento)args![1]!),
    _ => throw new Exception(name)
});
Task<int> Insert(Pagamento p) { p.Id = registros.Count + 1; registros.Add(p); return Task.FromResult(1); }
var hist = Stub.Make<IHistoricoRepositorio>((name, args) => {
    if (name == "HistoricoAsync") return Task.FromResult(new List<RegistroHistorico>());
    if (falharAuditoria) throw new InvalidOperationException("Falha simulada");
    auditorias++; return Task.FromResult(1);
});
var unidade = Stub.Make<IUnidadeTrabalho>((name, args) => Transaction((Func<Task>)args![1]!));
async Task Transaction(Func<Task> action) {
    transacoes++; var copy = registros.ToList();
    try { await action(); } catch { registros.Clear(); registros.AddRange(copy); throw; }
}
var demandas = Stub.Make<IDemandasRepositorio>((name, args) => name == "ProjetosAsync" ? Task.FromResult(new List<Projeto>()) : Task.FromResult(new List<Demanda> { new() { Id = 1, Titulo = "Demanda A" }, new() { Id = 2, Titulo = "Demanda B" }, new() { Id = 3, Titulo = "Sem horas" } }));
var apontamentos = Stub.Make<IApontamentosRepositorio>((name, args) => Task.FromResult(horas.ToList()));
var config = Stub.Make<IConfiguracoesRepositorio>((name, args) => Task.FromResult(tarifa));
var painel = new PainelServico(demandas, apontamentos, config, repo);
var servico = new PagamentosServico(repo, hist, unidade, painel, demandas);
void Check(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine("OK: " + message); }
async Task Reject(Func<Task> action, string message) {
    var count = registros.Count;
    try { await action(); throw new Exception("Aceitou solicitação inválida"); }
    catch (InvalidOperationException) { Check(registros.Count == count, message); }
}
BaixaPagamentos Selection(params int[] ids) => new() { Inicio = inicio, Fim = fim, DemandaIds = ids.ToList() };
FiltroPeriodo Filter(string? situacao = null) => new() { Inicio = inicio, Fim = fim, Pagamento = situacao };
var result = await servico.PainelAsync(7, Filter());
Check(result.DemandasPagamento.Count == 2 && result.Saldo == 300, "Demandas e saldos do período");
await Reject(() => servico.BaixarAsync(7, Selection()), "Rejeita seleção vazia");
await Reject(() => servico.BaixarAsync(7, Selection(1, 999)), "Rejeita demanda fora do filtro sem baixar parcialmente");
registros.Add(new() { Id = 1, DemandaId = 1, PeriodoInicio = inicio, PeriodoFim = fim, ValorPago = 50, DataPagamento = fim.AddDays(1) });
var parcial = await painel.PainelAsync(7, new() { Inicio = inicio, Fim = inicio, DemandaId = 1 });
Check(parcial.Pago == 25 && parcial.Saldo == 75 && parcial.Linhas.Single().SituacaoPagamento == "Parcial", "Filtro menor mantém parcela paga e saldo proporcional");
var before = transacoes;
await servico.BaixarAsync(7, Selection(1, 2, 2));
Check(registros.Count == 3 && registros[1].ValorPago == 150 && registros[2].ValorPago == 100, "Baixa múltipla desconta pagamento parcial e ignora IDs repetidos");
Check(transacoes == before + 1 && auditorias == 2, "Uma transação e auditoria por demanda");
Check(registros.Skip(1).All(p => p.DataPagamento == Horario.AgoraLocal.Date), "Data automática de Brasília");
await Reject(() => servico.BaixarAsync(7, Selection(1, 2)), "Reenvio não duplica demandas quitadas");
result = await servico.PainelAsync(7, Filter());
Check(result.Linhas.Count == 0 && result.DemandasPagamento.Count == 0 && result.Demandas.All(d => d.Id == 3), "Quitadas somem das listas operacionais e seleção de pagamento");
var historico = await painel.PainelAsync(7, Filter());
Check(historico.Linhas.Count == 3 && historico.Linhas.All(l => l.SituacaoPagamento == "Pago") && historico.Demandas.Count == 3, "Histórico preserva todas as demandas e status");
Check((await painel.PainelAsync(7, Filter("Pendente"))).Linhas.Count == 0, "Valores não exibem horas quitadas");
Check((await painel.PainelAsync(7, Filter("Pago"))).Linhas.Count == 3, "Relatório filtra somente pagos");
Check((await servico.PainelAsync(7, new() { Inicio = inicio, Fim = inicio })).DemandasPagamento.Count == 0, "Competência menor não reapresenta horas pagas");
horas.Add(new() { DemandaId = 1, Titulo = "Demanda A", Inicio = Horario.Utc(fim.AddHours(8)), Fim = Horario.Utc(fim.AddHours(9)) });
Check((await painel.PainelAsync(7, Filter())).Demandas.Single(d => d.Id == 1).SituacaoPagamento == "Parcial", "Novas horas deixam saldo e demanda volta como parcial");
horas.RemoveAt(horas.Count - 1);
var relatorios = new RelatoriosServico(painel);
using (var excel = new XLWorkbook(new MemoryStream(relatorios.GerarExcel(historico, "Teste")))) {
    Check(excel.Worksheet(1).Cell(7, 8).GetString() == "Pago", "Excel informa status de pagamento");
    Check(excel.Worksheet(1).Cell(7, 7).GetValue<decimal>() == 0, "Excel informa saldo quitado");
}
Check(relatorios.GerarPdf(historico, "Teste").Length > 1000, "PDF com status é gerado");
registros.Clear(); tarifa = null;
await Reject(() => servico.BaixarAsync(7, Selection(1)), "Sem valor/hora não calcula baixa automática");
registros.Add(new() { DemandaId = 1, PeriodoInicio = inicio, PeriodoFim = fim, ValorPago = 50 });
var semTarifa = await painel.PainelAsync(7, Filter());
Check(semTarifa.Pago == 50 && semTarifa.Linhas.Where(l => l.DemandaId == 1).All(l => l.SituacaoPagamento == "Parcial"), "Recebimentos sem tarifa não aparecem como não pagos");
registros.Clear();
tarifa = 100;
falharAuditoria = true;
await Reject(() => servico.BaixarAsync(7, Selection(1, 2)), "Falha no lote desfaz os registros");
falharAuditoria = false;
await servico.PagarAsync(7, new() { DemandaId = 1, PeriodoInicio = inicio, PeriodoFim = fim, ValorPago = 10, DataPagamento = inicio.AddYears(-1) });
Check(registros.Single().DataPagamento == Horario.AgoraLocal.Date, "Pagamento individual ignora data enviada");
await Reject(() => servico.PagarAsync(7, new() { DemandaId = 1, PeriodoInicio = inicio, PeriodoFim = fim, ValorPago = 300 }), "Pagamento individual não ultrapassa saldo");
// Distribuição com centavos deve conservar exatamente o valor recebido.
registros.Clear(); tarifa = 0.01m;
var centavos = await painel.PainelAsync(7, Filter());
registros.Add(new() { DemandaId = 1, PeriodoInicio = inicio, PeriodoFim = fim, ValorPago = 0.01m });
centavos = await painel.PainelAsync(7, Filter());
Check(centavos.Pago == 0.01m && centavos.Saldo == 0.02m, "Rateio preserva centavos");
tarifa = 100;
registros.Clear();
registros.Add(new() { Id = 1, DemandaId = 1, DemandaNome = "Demanda A", PeriodoInicio = inicio, PeriodoFim = fim, ValorPago = 200, DataPagamento = fim.AddDays(1) });
await ScreenChecks.RunAsync(painel, servico, repo, demandas, apontamentos, hist, unidade, inicio, fim);
Console.WriteLine("Todos os testes financeiros passaram. Nenhum banco real foi alterado.");

public class Stub : DispatchProxy {
    public Func<string, object?[]?, object?> Handler = null!;
    protected override object? Invoke(MethodInfo? method, object?[]? args) => Handler(method!.Name, args);
    public static T Make<T>(Func<string, object?[]?, object?> handler) where T : class {
        var value = Create<T, Stub>(); ((Stub)(object)value).Handler = handler; return value;
    }
}
