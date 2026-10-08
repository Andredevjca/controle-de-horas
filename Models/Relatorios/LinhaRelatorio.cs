namespace ControleHoras.Models;

public class LinhaRelatorio
{
    public decimal? Estimado { get; set; }
    public decimal Pago { get; set; }
    public decimal? Saldo => Estimado.HasValue ? Math.Max(0, Estimado.Value - Pago) : null;
    public string SituacaoPagamento => Pago > 0 && Saldo <= 0 ? "Pago" : Pago > 0 ? "Parcial" : "Não pago";
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
