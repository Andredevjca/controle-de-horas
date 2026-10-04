using ControleHoras.Models;
namespace ControleHoras.Interfaces.Repositories;

public interface IWhatsAppRepositorio
{
    Task<List<ContatoWhatsApp>> ContatosAsync(int usuarioId);
    Task<ContatoWhatsApp?> ContatoAsync(int usuarioId, int id);
    Task SalvarContatoAsync(int usuarioId, ContatoWhatsApp contato);
    Task<string> InstanciaAsync(int usuarioId, string prefixo);
    Task<(EnvioRelatorioWhatsApp Envio, bool Novo)> ReservarAsync(EnvioRelatorioWhatsApp envio);
    Task FinalizarAsync(int usuarioId, long id, ResultadoWhatsApp resultado);
    Task<EnvioRelatorioWhatsApp?> ObterEnvioAsync(int usuarioId, long id, bool incluirArquivo = false);
    Task<(List<EnvioRelatorioWhatsApp> Itens, int Total)> HistoricoAsync(int usuarioId, int pagina);
}
