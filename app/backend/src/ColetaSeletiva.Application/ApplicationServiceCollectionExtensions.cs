using ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ColetaSeletiva.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);
        services.AddScoped<RegistrarOrganizacaoHandler>();
        services.AddSingleton<TimeProvider>(TimeProvider.System);

        return services;
    }
}
