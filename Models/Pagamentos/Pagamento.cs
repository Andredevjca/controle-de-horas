using System.ComponentModel.DataAnnotations;

namespace ControleHoras.Models;

public class Pagamento
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Selecione a demanda do pagamento.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecione uma demanda válida.")]
    public int? DemandaId { get; set; }
    public string? DemandaNome { get; set; }
    public DateTime DataPagamento { get; set; } = DateTime.Today;
    public DateTime PeriodoInicio { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime PeriodoFim { get; set; } = DateTime.Today;
    [Range(typeof(decimal), "0.01", "99999999.99", ParseLimitsInInvariantCulture = true, ErrorMessage = "Informe um valor maior que zero.")]
    public decimal ValorPago { get; set; }
    [StringLength(2000)]
    public string? Observacao { get; set; }
}
