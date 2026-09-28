using ControleHoras.Models;
using ControleHoras.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControleHoras.Controllers;
[Authorize(Roles = "Administrador")]
public class UsuariosController(IUsuariosServico servico) : ControladorBase
{
    public async Task<IActionResult> Index() => View(await servico.UsuariosAsync());
    public async Task<IActionResult> Editar(int id = 0)
    {
        var usuario = id == 0 ? null : await servico.ObterUsuarioAsync(id);
        if (id != 0 && usuario == null) return NotFound();
        return View(usuario == null ? new EdicaoUsuario() : new EdicaoUsuario { Id = usuario.Id, Nome = usuario.Nome, Login = usuario.Login, Email = usuario.Email, Ativo = usuario.Ativo });
    }
    [HttpPost] public Task<IActionResult> Salvar(EdicaoUsuario usuario) => ExecutarAsync(() => servico.SalvarUsuarioAsync(UsuarioId, usuario), "/Usuarios");
}
