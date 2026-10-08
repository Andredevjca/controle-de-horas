namespace ControleHoras.Models;

public class Painel
{
    public List<DemandaPagamento> DemandasPagamento { get; set; } = [];
    public List<Demanda> Demandas { get; set; } = [];
    public List<Projeto> Projetos { get; set; } = [];
    public List<Pagamento> Pagamentos { get; set; } = [];
    public List<LinhaRelatorio> Linhas { get; set; } = [];
    public FiltroPeriodo Filtro { get; set; } = new();
    public decimal? ValorHora { get; set; }
    public double SegundosHoje { get; set; }
    public double SegundosMes { get; set; }
    public double SegundosTotais { get; set; }
    public double Segundos => Linhas.Sum(l => l.Segundos);
    public decimal? Estimado => ValorHora.HasValue ? Linhas.Sum(l => l.Estimado ?? l.Valor(ValorHora) ?? 0) : null;
    public decimal Pago => Linhas.Sum(l => l.Pago);
    public decimal? Saldo => ValorHora.HasValue ? Linhas.Sum(l => l.Saldo ?? l.Valor(ValorHora) ?? 0) : null;
    public decimal? SaldoGeral { get; set; }
    public Demanda? Trabalho => Demandas.FirstOrDefault(d => d.Status == "Em andamento") ?? Demandas.FirstOrDefault(d => d.Status == "Pausada");
}
