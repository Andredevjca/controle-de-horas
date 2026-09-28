namespace ControleHoras.Models;

public class RegistroHistorico { public DateTime Data { get; set; } public string Acao { get; set; } = ""; public string? Detalhes { get; set; } public int? DemandaId { get; set; } }


