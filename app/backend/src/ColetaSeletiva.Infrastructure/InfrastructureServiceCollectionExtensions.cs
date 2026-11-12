using ColetaSeletiva.Application.Seguranca;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
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

        services.AddDbContext<ColetaSeletivaDbContext>(options =>
            options.UseSqlServer(connectionString));

        services
            .AddIdentityCore<Usuario>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ColetaSeletivaDbContext>();

        return services;
    }
}
