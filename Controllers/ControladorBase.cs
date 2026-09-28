using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControleHoras.Controllers;
[Authorize]
public abstract class ControladorBase : Controller
{
    protected int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected async Task<IActionResult> ExecutarAsync(Func<Task> operacao, string destino)
    {
        if (!ModelState.IsValid) { TempData["Erro"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Verifique os campos informados." : e.ErrorMessage)); return LocalRedirect(destino); }
        try { await operacao(); TempData["Sucesso"] = "Alterações salvas com sucesso."; }
        catch (InvalidOperationException erro) { TempData["Erro"] = erro.Message; }
        catch (MySqlConnector.MySqlException erro) when (erro.Number == 1062) { TempData["Erro"] = "Já existe um registro com esses dados. Atualize a página e tente novamente."; }
        return LocalRedirect(destino);
    }
}
