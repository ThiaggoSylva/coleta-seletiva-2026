using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia.Conversores;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.Infrastructure.Persistencia;

public sealed class ColetaSeletivaDbContext : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>
{
    public ColetaSeletivaDbContext(DbContextOptions<ColetaSeletivaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Organizacao> Organizacoes => Set<Organizacao>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Auditoria> Auditorias => Set<Auditoria>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ColetaSeletivaDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<UtcDateTimeOffsetConverter>();
    }
}
