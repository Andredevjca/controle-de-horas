using System.ComponentModel.DataAnnotations;

namespace ControleHoras.Models;

public class Demanda
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    [Required(ErrorMessage = "Informe o título."), StringLength(180)]
    public string Titulo { get; set; } = "";
    [StringLength(10000)]
    public string? Descricao { get; set; }
    public int? ProjetoId { get; set; }
    public string? ProjetoNome { get; set; }
    [Required, RegularExpression("Baixa|Normal|Alta|Urgente")]
    public string Prioridade { get; set; } = "Normal";
    [Required, RegularExpression("Pendente|Em andamento|Pausada|Concluída|Cancelada")]
    public string Status { get; set; } = "Pendente";
    public DateTime DataRecebimento { get; set; } = DateTime.Today;
    public DateTime? Prazo { get; set; }
    [StringLength(10000)]
    public string? Observacoes { get; set; }
    public DateTime? DataFinalizacao { get; set; }
    public long Segundos { get; set; }
    public string Codigo => $"DEV-{Id:000}";
}
