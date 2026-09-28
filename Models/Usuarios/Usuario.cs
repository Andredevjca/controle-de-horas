using System.ComponentModel.DataAnnotations;

namespace ControleHoras.Models;

public class Usuario
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Informe o nome."), StringLength(120)]
    public string Nome { get; set; } = "";
    [Required(ErrorMessage = "Informe o usuário."), StringLength(60)]
    public string Login { get; set; } = "";
    [EmailAddress(ErrorMessage = "Informe um e-mail válido."), StringLength(180)]
    public string? Email { get; set; }
    public string Senha { get; set; } = "";
    public bool Ativo { get; set; } = true;
    public bool Administrador { get; set; }
    public int VersaoSessao { get; set; }
}
