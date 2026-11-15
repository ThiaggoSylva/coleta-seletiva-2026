using ColetaSeletiva.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class MunicipioConfiguration : IEntityTypeConfiguration<Municipio>
{
    public void Configure(EntityTypeBuilder<Municipio> builder)
    {
        builder.ToTable("Municipio");
        builder.HasKey(municipio => municipio.Id);
        builder.Property(municipio => municipio.Id).ValueGeneratedNever();
        builder
            .Property(municipio => municipio.Nome)
            .HasMaxLength(Municipio.NomeMaxLength)
            .IsRequired();
        builder
            .Property(municipio => municipio.Uf)
            .HasMaxLength(Municipio.UfLength)
            .IsRequired();

        builder
            .HasIndex(municipio => new { municipio.Uf, municipio.Nome })
            .HasDatabaseName("IX_Municipio_Uf_Nome");
    }
}
