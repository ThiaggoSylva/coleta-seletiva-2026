using ColetaSeletiva.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class OrganizacaoTipoConfiguration : IEntityTypeConfiguration<OrganizacaoTipo>
{
    public void Configure(EntityTypeBuilder<OrganizacaoTipo> builder)
    {
        builder.ToTable("OrganizacaoTipo");
        builder.HasKey(vinculo => vinculo.Id);
        builder.Property(vinculo => vinculo.Id).ValueGeneratedNever();
        builder.Property(vinculo => vinculo.OrganizacaoId).IsRequired();
        builder.Property(vinculo => vinculo.TipoResiduoId).IsRequired();
        builder.Property(vinculo => vinculo.AceitaRetiradaDomiciliar).IsRequired();
        builder.Property(vinculo => vinculo.CriadoEmUtc).IsRequired();
        builder.Property(vinculo => vinculo.AtualizadoEmUtc).IsRequired();
        builder.Property(vinculo => vinculo.RowVersion).IsRowVersion();

        builder
            .HasAlternateKey(vinculo => new { vinculo.OrganizacaoId, vinculo.TipoResiduoId })
            .HasName("AK_OrganizacaoTipo_OrganizacaoId_TipoResiduoId");

        builder
            .HasOne<Organizacao>()
            .WithMany()
            .HasForeignKey(vinculo => vinculo.OrganizacaoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<TipoResiduo>()
            .WithMany()
            .HasForeignKey(vinculo => vinculo.TipoResiduoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
