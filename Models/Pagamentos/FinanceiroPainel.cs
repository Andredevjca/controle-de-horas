namespace ControleHoras.Models;

public class FinanceiroPainel
{
    public FiltroPeriodo Filtro { get; set; } = new();
    public List<Pagamento> Recebimentos { get; set; } = [];
    public List<FinanceiroMes> Meses { get; set; } = [];
    public int Demandas { get; set; }
    public int DemandasRecebidas => Recebimentos.Where(p => p.DemandaId.HasValue).Select(p => p.DemandaId).Distinct().Count();
    public decimal Recebido => Recebimentos.Sum(p => p.ValorPago);
    public double Segundos { get; set; }
    public decimal? Saldo { get; set; }
    public decimal TotalRecebidoGeral { get; set; }
}
public class FinanceiroMes
{
    public DateTime Mes { get; set; }
    public int DemandasRecebidas { get; set; }
    public int QuantidadeBaixas { get; set; }
    public decimal Recebido { get; set; }
    public string Situacao => QuantidadeBaixas > 0 ? "Com baixa" : "Sem baixa";
}
