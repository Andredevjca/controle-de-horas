namespace ControleHoras.Models;

public class DetalhesDemanda
{
    public Demanda Demanda { get; set; } = new();
    public List<HorasDia> Dias { get; set; } = [];
    public double SegundosTotais => Dias.Sum(d => d.Segundos);
}
