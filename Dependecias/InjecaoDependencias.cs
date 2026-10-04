using ControleHoras.Data;
using ControleHoras.Interfaces.Repositories;
using ControleHoras.Interfaces.Services;
using ControleHoras.Models;
using ControleHoras.Repositories;
using ControleHoras.Services;
using Microsoft.AspNetCore.Identity;

namespace ControleHoras.Dependecias;

public static class InjecaoDependencias
{
    public static IServiceCollection AdicionarDependencias(this IServiceCollection services)
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddSingleton<BancoDados>();
        services.AddScoped<SessaoBanco>();
        services.AddScoped<IUnidadeTrabalho>(provider => provider.GetRequiredService<SessaoBanco>());
        services.AddScoped<IUsuariosRepositorio, UsuariosRepositorio>();
        services.AddScoped<IDemandasRepositorio, DemandasRepositorio>();
        services.AddScoped<IApontamentosRepositorio, ApontamentosRepositorio>();
        services.AddScoped<IConfiguracoesRepositorio, ConfiguracoesRepositorio>();
        services.AddScoped<IPagamentosRepositorio, PagamentosRepositorio>();
        services.AddScoped<IHistoricoRepositorio, HistoricoRepositorio>();
        services.AddScoped<IPainelServico, PainelServico>();
        services.AddScoped<IDemandasServico, DemandasServico>();
        services.AddScoped<IApontamentosServico, ApontamentosServico>();
        services.AddScoped<IConfiguracoesServico, ConfiguracoesServico>();
        services.AddScoped<IPagamentosServico, PagamentosServico>();
        services.AddScoped<IUsuariosServico, UsuariosServico>();
        services.AddScoped<IDashboardServico, DashboardServico>();
        services.AddScoped<IRelatoriosServico, RelatoriosServico>();
        services.AddScoped<IWhatsAppRepositorio, WhatsAppRepositorio>();
        services.AddHttpClient<IWhatsAppGateway, EvolutionWhatsAppGateway>();
        services.AddScoped<WhatsAppRelatoriosServico>();
        services.AddScoped<IAutenticacaoServico, AutenticacaoServico>();
        return services;
    }
}
