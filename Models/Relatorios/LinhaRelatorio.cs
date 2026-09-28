namespace ControleHoras.Models;

public class LinhaRelatorio
{
    public DateTime Data { get; set; }
    public int DemandaId { get; set; }
    public string Titulo { get; set; } = "";
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public bool EmAndamento { get; set; }
    public string? Descricao { get; set; }
    public double Segundos => (Fim - Inicio).TotalSeconds;
    public decimal? Valor(decimal? tarifa) => tarifa.HasValue ? (decimal)Segundos / 3600m * tarifa.Value : null;
}
