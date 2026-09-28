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
        services.AddScoped<IControleRepositorio, Repositorio>();
        services.AddScoped<IControleServico, ControleServico>();
        return services;
    }
}
