using System.ComponentModel.DataAnnotations;

namespace ControleHoras.Models;

public class EdicaoUsuario : Usuario
{
    [MinLength(8, ErrorMessage = "A nova senha deve ter pelo menos 8 caracteres.")]
    public string? NovaSenha { get; set; }
    [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não coincidem.")]
    public string? ConfirmarSenha { get; set; }
}
