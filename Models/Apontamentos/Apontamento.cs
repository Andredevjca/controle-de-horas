namespace ControleHoras.Models;

public class Apontamento
{
    public int Id { get; set; }
    public int DemandaId { get; set; }
    public int UsuarioId { get; set; }
    public string Titulo { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public string? Descricao { get; set; }
    public bool LancamentoManual { get; set; }
}
