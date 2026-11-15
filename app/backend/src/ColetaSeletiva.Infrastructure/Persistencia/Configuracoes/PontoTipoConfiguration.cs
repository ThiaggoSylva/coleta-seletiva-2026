using ColetaSeletiva.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class PontoTipoConfiguration : IEntityTypeConfiguration<PontoTipo>
{
    public void Configure(EntityTypeBuilder<PontoTipo> builder)
    {
        builder.ToTable("PontoTipo");
        builder.HasKey(tipo => tipo.Id);
        builder.Property(tipo => tipo.Id).ValueGeneratedNever();
        builder.Property(tipo => tipo.OrganizacaoId).IsRequired();
        builder.Property(tipo => tipo.PontoColetaId).IsRequired();
        builder.Property(tipo => tipo.TipoResiduoId).IsRequired();

        builder
            .HasIndex(tipo => new { tipo.OrganizacaoId, tipo.PontoColetaId, tipo.TipoResiduoId })
            .IsUnique()
            .HasDatabaseName("UX_PontoTipo_OrganizacaoId_PontoColetaId_TipoResiduoId");

        builder
            .HasOne<OrganizacaoTipo>()
            .WithMany()
            .HasForeignKey(tipo => new { tipo.OrganizacaoId, tipo.TipoResiduoId })
            .HasPrincipalKey(vinculo => new { vinculo.OrganizacaoId, vinculo.TipoResiduoId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
