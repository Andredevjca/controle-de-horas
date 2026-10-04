using System.ComponentModel.DataAnnotations;

namespace ControleHoras.Models;

public sealed class ContatoWhatsApp
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Nome { get; set; } = "";
    [Required, StringLength(30)] public string Telefone { get; set; } = "";
    [Required, StringLength(1000)] public string Template { get; set; } = "Olá, {cliente}! Segue o relatório de {inicio} a {fim}. Total de horas: {horas}. Saldo: {saldo}.";
}

public sealed class EnviarRelatorioWhatsApp
{
    [Range(1, int.MaxValue)] public int ContatoId { get; set; }
    [Required, RegularExpression("^(pdf|excel)$")] public string Tipo { get; set; } = "pdf";
    public Guid Chave { get; set; }
    public FiltroPeriodo Filtro { get; set; } = new();
}

public sealed class EnvioRelatorioWhatsApp
{
    public long Id { get; set; }
    public int UsuarioId { get; set; }
    public Guid Chave { get; set; }
    public string Nome { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Template { get; set; } = "";
    public string Mensagem { get; set; } = "";
    public string Instancia { get; set; } = "";
    public string Tipo { get; set; } = "";
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public int? DemandaId { get; set; }
    public string? FiltroStatus { get; set; }
    public string NomeArquivo { get; set; } = "";
    public string Mime { get; set; } = "";
    public byte[] Arquivo { get; set; } = [];
    public string Status { get; set; } = "Enviando";
    public string? EvolutionId { get; set; }
    public string? Erro { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? FinalizadoEm { get; set; }
}

public sealed class WhatsAppPagina
{
    public FiltroPeriodo Filtro { get; set; } = new();
    public List<ContatoWhatsApp> Contatos { get; set; } = [];
    public List<EnvioRelatorioWhatsApp> Historico { get; set; } = [];
    public ContatoWhatsApp Edicao { get; set; } = new();
    public int Pagina { get; set; } = 1;
    public int Total { get; set; }
    public bool Configurado { get; set; }
    public Painel Resumo { get; set; } = new();
}

public sealed record ContaWhatsApp(string Instancia, bool Conectado, string? Numero = null, string? Qrcode = null);
public sealed record ResultadoWhatsApp(string Status, string? Id = null, string? Erro = null);
