using System.ComponentModel.DataAnnotations;
namespace ControleHoras.Models;

public class BaixaPagamentos
{
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public List<int> DemandaIds { get; set; } = [];
    [StringLength(2000)] public string? Observacao { get; set; }
}

public class DemandaPagamento
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public double Segundos { get; set; }
    public decimal? Estimado { get; set; }
    public decimal Pago { get; set; }
    public bool CompetenciaSobreposta { get; set; }
    public decimal? Saldo => Estimado.HasValue ? Math.Max(0, Estimado.Value - Pago) : null;
    public bool PodePagar => Saldo > 0 && !CompetenciaSobreposta;
}
