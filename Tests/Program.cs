using System.Reflection;
using ClosedXML.Excel;
using ControleHoras.Data;
using ControleHoras.Dependecias;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using ControleHoras.Models;
using ControleHoras.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var verificacoes = 0;
void Verificar(bool condicao, string descricao)
{
    if (!condicao) throw new Exception(descricao);
    verificacoes++;
    Console.WriteLine($"OK: {descricao}");
}

// Resolve as dependências reais sem abrir uma conexão ou inicializar o banco.
var builder = WebApplication.CreateBuilder();
builder.Configuration["ConnectionStrings:BancoDados"] = "Server=localhost;Database=controle_horas_testes;User ID=testes;";
builder.Services.AdicionarDependencias();
using var app = builder.Build();
using (var escopo = app.Services.CreateScope())
{
    var provider = escopo.ServiceProvider;
    foreach (var contrato in typeof(IPainelServico).Assembly.GetTypes().Where(t => t.IsInterface && t.Namespace == typeof(IPainelServico).Namespace))
        Verificar(provider.GetRequiredService(contrato) != null, $"Resolução de {contrato.Name}");
    Verificar(ReferenceEquals(provider.GetRequiredService<SessaoBanco>(), provider.GetRequiredService<IUnidadeTrabalho>()), "Unidade de trabalho compartilha a sessão do escopo");
    using var outro = app.Services.CreateScope();
    Verificar(!ReferenceEquals(provider.GetRequiredService<SessaoBanco>(), outro.ServiceProvider.GetRequiredService<SessaoBanco>()), "Requisições diferentes possuem sessões independentes");
}

var dia = new DateTime(2026, 1, 15);
var registros = new List<Apontamento> { new() { DemandaId = 7, Titulo = "Entrega", Status = "Concluída", Inicio = Horario.Utc(dia.AddHours(23)), Fim = Horario.Utc(dia.AddDays(1).AddHours(1)) } };
var linhas = CalculoHoras.Recortar(registros, dia, dia.AddDays(1));
Verificar(linhas.Count == 2 && linhas.All(l => l.Segundos == 3600), "Intervalo atravessando meia-noite é dividido em dois dias");
Verificar(CalculoHoras.Recortar(registros, dia, dia).Sum(l => l.Segundos) == 3600, "Recorte respeita o limite do período");

decimal? tarifa = 100m;
var demandas = Substituto.Criar<IDemandasRepositorio>((nome, _) => nome switch {
    "DemandasAsync" => Task.FromResult(new List<Demanda> { new() { Id = 7, Titulo = "Entrega" } }),
    "ProjetosAsync" => Task.FromResult(new List<Projeto>()),
    _ => throw new NotSupportedException(nome)
});
var apontamentos = Substituto.Criar<IApontamentosRepositorio>((_, _) => Task.FromResult(registros));
var configuracoes = Substituto.Criar<IConfiguracoesRepositorio>((_, _) => Task.FromResult(tarifa));
var pagamentos = Substituto.Criar<IPagamentosRepositorio>((_, _) => Task.FromResult(new List<Pagamento> {
    new() { PeriodoInicio = dia, PeriodoFim = dia.AddDays(1), ValorPago = 50m },
    new() { PeriodoInicio = dia.AddDays(-1), PeriodoFim = dia, ValorPago = 25m }
}));
var painelServico = new PainelServico(demandas, apontamentos, configuracoes, pagamentos);
var filtro = new FiltroPeriodo { Inicio = dia, Fim = dia.AddDays(1) };
var painel = await painelServico.PainelAsync(1, filtro);
Verificar(painel.Segundos == 7200 && painel.Estimado == 200m && painel.Pago == 50m && painel.Saldo == 150m, "Cálculo de horas, tarifa e competência dos pagamentos");
Verificar(painel.SaldoGeral == 125m, "Saldo global inclui todos os pagamentos");
tarifa = null;
painel = await painelServico.PainelAsync(1, filtro);
Verificar(painel.Estimado == null && painel.SaldoGeral == null && painel.Segundos == 7200, "Tarifa ausente preserva as horas sem estimar valores");
try
{
    await painelServico.PainelAsync(1, new FiltroPeriodo { Inicio = dia, Fim = dia.AddDays(-1) });
    throw new Exception("Período inválido foi aceito");
}
catch (InvalidOperationException) { Verificar(true, "Período invertido é rejeitado"); }
tarifa = 100m;
var relatorios = new RelatoriosServico(painelServico);
using var fluxo = new MemoryStream(await relatorios.ExportarExcelAsync(1, filtro, "Pessoa de teste"));
using var arquivo = new XLWorkbook(fluxo);
Verificar(arquivo.Worksheets.Count == 2 && arquivo.Worksheet(1).Cell(2, 1).GetString() == "Pessoa de teste", "Exportação Excel mantém planilhas e identificação");
Verificar(arquivo.Worksheet(2).Cell(2, 3).GetValue<decimal>() == 50m, "Excel inclui apenas pagamentos da competência selecionada");
Console.WriteLine($"{verificacoes} verificações concluídas sem acesso ao banco.");

public class Substituto : DispatchProxy
{
    public Func<string, object?[]?, object?> Responder { get; set; } = null!;
    protected override object? Invoke(MethodInfo? metodo, object?[]? argumentos) => Responder(metodo!.Name, argumentos);
    public static T Criar<T>(Func<string, object?[]?, object?> responder) where T : class
    {
        var instancia = Create<T, Substituto>();
        ((Substituto)(object)instancia).Responder = responder;
        return instancia;
    }
}
