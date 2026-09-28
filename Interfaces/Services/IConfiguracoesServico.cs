using ControleHoras.Models;

namespace ControleHoras.Interfaces.Services;

public interface IConfiguracoesServico
{
    Task DefinirValorAsync(int usuarioId, decimal? valor);
    Task<decimal?> ValorHoraAsync(int usuarioId);
    Task AlterarSenhaAsync(int usuarioId, string senhaAtual, string novaSenha, string confirmarSenha);
}
