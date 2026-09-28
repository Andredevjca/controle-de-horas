namespace ControleHoras.Models;

public class FiltroPeriodo
{
    public DateTime Inicio { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime Fim { get; set; } = DateTime.Today;
    public int? DemandaId { get; set; }
    public string? Status { get; set; }
}
