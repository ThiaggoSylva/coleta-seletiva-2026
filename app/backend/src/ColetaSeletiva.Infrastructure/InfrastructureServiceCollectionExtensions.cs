using ColetaSeletiva.Application.Acesso;
using ColetaSeletiva.Application.Acesso.Equipe;
using ColetaSeletiva.Application.Acesso.PerfilDoCidadao;
using ColetaSeletiva.Application.Acesso.Sessao;
using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Seguranca;
using ColetaSeletiva.Infrastructure.Acesso;
using ColetaSeletiva.Infrastructure.AuditoriaDeEventos;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.Infrastructure.Seguranca;
using ColetaSeletiva.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ColetaSeletiva.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(provedor =>
            provedor.GetRequiredService<TenantContext>());

        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
        services.AddScoped<IRegistroDeOrganizacoes, RegistroDeOrganizacoes>();
        services.AddScoped<UnicidadeDeContas>();
        services.AddScoped<ICadastroDeContas, CadastroDeContas>();
        services.AddScoped<IGerenciamentoDeEquipe, GerenciamentoDeEquipe>();
        services.AddScoped<IPerfilDeCidadao, PerfilDeCidadao>();
        services.AddScoped<IRegistroDeAuditoria, RegistroDeAuditoria>();
        services.AddScoped<IAutenticacaoDeContas, AutenticacaoDeContas>();
        services.AddScoped<IAlteracaoDeSenha, AlteracaoDeSenha>();
        services.AddScoped<IRepositorioDeRefreshTokens, RepositorioDeRefreshTokens>();
        services.AddSingleton<IGeradorDeTokenDeAcesso, GeradorDeTokenDeAcesso>();

        services.AddDbContext<ColetaSeletivaDbContext>(options =>
            options.UseSqlServer(connectionString));

        services
            .AddIdentityCore<Usuario>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ColetaSeletivaDbContext>();

        return services;
    }
}
