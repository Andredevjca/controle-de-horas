using ControleHoras.Models;
namespace ControleHoras.Interfaces.Services;

public interface IWhatsAppGateway
{
    bool Configurado { get; }
    string Prefixo { get; }
    Task<ContaWhatsApp> StatusAsync(string instancia);
    Task<ContaWhatsApp> ConectarAsync(string instancia);
    Task DesconectarAsync(string instancia);
    Task<ResultadoWhatsApp> EnviarAsync(EnvioRelatorioWhatsApp envio);
}
