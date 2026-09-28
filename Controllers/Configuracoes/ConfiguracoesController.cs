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
public class ConfiguracoesController(IControleServico servico, IControleRepositorio repositorio, IPasswordHasher<Usuario> senhas) : ControladorBase
{
    public async Task<IActionResult> Index() { ViewBag.ValorHora = await repositorio.ValorHoraAsync(UsuarioId); return View(); }
    [HttpPost] public Task<IActionResult> Salvar(decimal? valorHora) => ExecutarAsync(() => servico.DefinirValorAsync(UsuarioId, valorHora), "/Configuracoes");
    [HttpPost] public Task<IActionResult> Senha(string senhaAtual, string novaSenha, string confirmarSenha) => ExecutarAsync(async () => {
        var usuario = (await repositorio.ObterUsuarioAsync(UsuarioId))!;
        ControleServico.Exigir(senhas.VerifyHashedPassword(usuario, usuario.Senha, senhaAtual) != PasswordVerificationResult.Failed, "Senha atual incorreta.");
        ControleServico.Exigir(novaSenha.Length >= 8 && novaSenha == confirmarSenha, "Use pelo menos 8 caracteres e confirme a nova senha corretamente.");
        await servico.SalvarUsuarioAsync(UsuarioId, new EdicaoUsuario { Id = usuario.Id, Nome = usuario.Nome, Login = usuario.Login, Email = usuario.Email, Ativo = usuario.Ativo, NovaSenha = novaSenha });
        await HttpContext.SignOutAsync();
    }, "/Configuracoes");
}

