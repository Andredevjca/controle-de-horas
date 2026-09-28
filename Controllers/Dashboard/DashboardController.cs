using ControleHoras.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleHoras.Controllers;
public class DashboardController(IDashboardServico servico) : ControladorBase
{
    public async Task<IActionResult> Index() => View(await servico.PainelAsync(UsuarioId));
}
