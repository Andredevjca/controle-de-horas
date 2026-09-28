namespace ControleHoras.Models;

public class Painel
{
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
    public decimal? Estimado => ValorHora.HasValue ? (decimal)Segundos / 3600m * ValorHora.Value : null;
    public decimal Pago => Pagamentos.Sum(p => p.ValorPago);
    public decimal? Saldo => Estimado - Pago;
    public decimal? SaldoGeral { get; set; }
    public Demanda? Trabalho => Demandas.FirstOrDefault(d => d.Status == "Em andamento") ?? Demandas.FirstOrDefault(d => d.Status == "Pausada");
}
