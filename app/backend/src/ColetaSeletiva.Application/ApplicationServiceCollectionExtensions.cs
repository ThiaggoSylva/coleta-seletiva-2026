using ColetaSeletiva.Application.Acesso.Equipe;
using ColetaSeletiva.Application.Acesso.PerfilDoCidadao;
using ColetaSeletiva.Application.Acesso.RegistroDeCidadao;
using ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;
using ColetaSeletiva.Application.Acesso.Sessao;
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
        services.AddScoped<RegistrarCidadaoHandler>();
        services.AddScoped<ObterPerfilDoCidadaoHandler>();
        services.AddScoped<AtualizarPerfilDoCidadaoHandler>();
        services.AddScoped<IniciarSessaoHandler>();
        services.AddScoped<RenovarSessaoHandler>();
        services.AddScoped<EncerrarSessaoHandler>();
        services.AddScoped<AlterarSenhaHandler>();
        services.AddScoped<ObterContaAtualHandler>();
        services.AddScoped<CriarOperadorHandler>();
        services.AddScoped<ObterOperadorHandler>();
        services.AddScoped<EditarOperadorHandler>();
        services.AddSingleton<TimeProvider>(TimeProvider.System);

        return services;
    }
}
