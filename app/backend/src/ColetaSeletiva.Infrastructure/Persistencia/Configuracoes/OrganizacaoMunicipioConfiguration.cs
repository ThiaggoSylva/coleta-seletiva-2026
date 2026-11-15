using ColetaSeletiva.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class OrganizacaoMunicipioConfiguration : IEntityTypeConfiguration<OrganizacaoMunicipio>
{
    public void Configure(EntityTypeBuilder<OrganizacaoMunicipio> builder)
    {
        builder.ToTable("OrganizacaoMunicipio");
        builder.HasKey(vinculo => vinculo.Id);
        builder.Property(vinculo => vinculo.Id).ValueGeneratedNever();
        builder.Property(vinculo => vinculo.OrganizacaoId).IsRequired();
        builder.Property(vinculo => vinculo.MunicipioId).IsRequired();
        builder.Property(vinculo => vinculo.CriadoEmUtc).IsRequired();

        builder
            .HasAlternateKey(vinculo => new { vinculo.OrganizacaoId, vinculo.MunicipioId })
            .HasName("AK_OrganizacaoMunicipio_OrganizacaoId_MunicipioId");

        builder
            .HasOne<Organizacao>()
            .WithMany()
            .HasForeignKey(vinculo => vinculo.OrganizacaoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<Municipio>()
            .WithMany()
            .HasForeignKey(vinculo => vinculo.MunicipioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
