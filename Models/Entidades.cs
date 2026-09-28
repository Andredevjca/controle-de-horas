using System.ComponentModel.DataAnnotations;

namespace ControleHoras.Models;

public class Usuario
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Informe o nome."), StringLength(120)] public string Nome { get; set; } = "";
    [Required(ErrorMessage = "Informe o usuário."), StringLength(60)] public string Login { get; set; } = "";
    [EmailAddress(ErrorMessage = "Informe um e-mail válido."), StringLength(180)] public string? Email { get; set; }
    public string Senha { get; set; } = "";
    public bool Ativo { get; set; } = true;
    public bool Administrador { get; set; }
    public int VersaoSessao { get; set; }
}
public class EdicaoUsuario : Usuario
{
    [MinLength(8, ErrorMessage = "A nova senha deve ter pelo menos 8 caracteres.")]
    public string? NovaSenha { get; set; }
    [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não coincidem.")] public string? ConfirmarSenha { get; set; }
}
public class Demanda
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    [Required(ErrorMessage = "Informe o título."), StringLength(180)] public string Titulo { get; set; } = "";
    [StringLength(10000)] public string? Descricao { get; set; }
    public int? ProjetoId { get; set; }
    public string? ProjetoNome { get; set; }
    [Required, RegularExpression("Baixa|Normal|Alta|Urgente")] public string Prioridade { get; set; } = "Normal";
    [Required, RegularExpression("Pendente|Em andamento|Pausada|Concluída|Cancelada")] public string Status { get; set; } = "Pendente";
    public DateTime DataRecebimento { get; set; } = DateTime.Today;
    public DateTime? Prazo { get; set; }
    [StringLength(10000)] public string? Observacoes { get; set; }
    public DateTime? DataFinalizacao { get; set; }
    public long Segundos { get; set; }
    public string Codigo => $"DEV-{Id:000}";
}
public class HorasDia
{
    public DateTime Data { get; set; }
    public double Segundos { get; set; }
}
public class DetalhesDemanda
{
    public Demanda Demanda { get; set; } = new();
    public List<HorasDia> Dias { get; set; } = [];
    public double SegundosTotais => Dias.Sum(d => d.Segundos);
}
public class Projeto { public int Id { get; set; } public string Nome { get; set; } = ""; }
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
public class LancamentoManual
{
    [Range(1, int.MaxValue)] public int DemandaId { get; set; }
    public DateTime Data { get; set; } = DateTime.Today;
    public TimeSpan Inicio { get; set; }
    public TimeSpan Fim { get; set; }
    [StringLength(2000)] public string? Descricao { get; set; }
}
public class Pagamento
{
    public int Id { get; set; }
    public DateTime DataPagamento { get; set; } = DateTime.Today;
    public DateTime PeriodoInicio { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime PeriodoFim { get; set; } = DateTime.Today;
    [Range(typeof(decimal), "0.01", "99999999.99", ParseLimitsInInvariantCulture = true, ErrorMessage = "Informe um valor maior que zero.")] public decimal ValorPago { get; set; }
    [StringLength(2000)] public string? Observacao { get; set; }
}
public class FiltroPeriodo
{
    public DateTime Inicio { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime Fim { get; set; } = DateTime.Today;
    public int? DemandaId { get; set; }
    public string? Status { get; set; }
}
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
public class Painel
{
    public List<Demanda> Demandas { get; set; } = [];
    public List<Projeto> Projetos { get; set; } = [];
    public List<Pagamento> Pagamentos { get; set; } = [];
    public List<LinhaRelatorio> Linhas { get; set; } = [];
    public FiltroPeriodo Filtro { get; set; } = new();
    public decimal? ValorHora { get; set; }
    public double SegundosHoje { get; set; }
    public double SegundosMes { get; set; }
    public double SegundosTotais { get; set; }
    public double Segundos => Linhas.Sum(l => l.Segundos);
    public decimal? Estimado => ValorHora.HasValue ? (decimal)Segundos / 3600m * ValorHora.Value : null;
    public decimal Pago => Pagamentos.Sum(p => p.ValorPago);
    public decimal? Saldo => Estimado - Pago;
    public decimal? SaldoGeral { get; set; }
    public Demanda? Trabalho => Demandas.FirstOrDefault(d => d.Status == "Em andamento") ?? Demandas.FirstOrDefault(d => d.Status == "Pausada");
}
public static class Formato
{
    public static string Horas(double segundos) => $"{(int)(segundos / 3600):00}h {(int)(segundos % 3600 / 60):00}min";
    public static string Dinheiro(decimal? valor) => valor?.ToString("C2") ?? "—";
}


