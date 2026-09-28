using System.Security.Claims;
using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using ControleHoras.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ClosedXML.Excel;

namespace ControleHoras.Controllers;
[Authorize(Roles = "Administrador")]
public class UsuariosController(IControleServico servico, IControleRepositorio repositorio) : ControladorBase
{
    public async Task<IActionResult> Index() => View(await repositorio.UsuariosAsync());
    public async Task<IActionResult> Editar(int id = 0)
    {
        var usuario = id == 0 ? null : await repositorio.ObterUsuarioAsync(id);
        if (id != 0 && usuario == null) return NotFound();
        return View(usuario == null ? new EdicaoUsuario() : new EdicaoUsuario { Id = usuario.Id, Nome = usuario.Nome, Login = usuario.Login, Email = usuario.Email, Ativo = usuario.Ativo });
    }
    [HttpPost] public Task<IActionResult> Salvar(EdicaoUsuario usuario) => ExecutarAsync(() => servico.SalvarUsuarioAsync(UsuarioId, usuario), "/Usuarios");
}

