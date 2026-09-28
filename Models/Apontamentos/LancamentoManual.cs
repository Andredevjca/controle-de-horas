using System.ComponentModel.DataAnnotations;

namespace ControleHoras.Models;

public class LancamentoManual
{
    [Range(1, int.MaxValue)]
    public int DemandaId { get; set; }
    public DateTime Data { get; set; } = DateTime.Today;
    public TimeSpan Inicio { get; set; }
    public TimeSpan Fim { get; set; }
    [StringLength(2000)]
    public string? Descricao { get; set; }
}
