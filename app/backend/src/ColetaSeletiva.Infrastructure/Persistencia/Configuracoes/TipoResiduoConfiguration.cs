using ColetaSeletiva.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class TipoResiduoConfiguration : IEntityTypeConfiguration<TipoResiduo>
{
    public void Configure(EntityTypeBuilder<TipoResiduo> builder)
    {
        builder.ToTable("TipoResiduo");
        builder.HasKey(tipo => tipo.Id);
        builder.Property(tipo => tipo.Id).ValueGeneratedNever();
        builder
            .Property(tipo => tipo.Codigo)
            .HasMaxLength(TipoResiduo.CodigoMaxLength)
            .IsRequired();
        builder
            .Property(tipo => tipo.Nome)
            .HasMaxLength(TipoResiduo.NomeMaxLength)
            .IsRequired();
        builder
            .Property(tipo => tipo.Categoria)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder
            .Property(tipo => tipo.Unidade)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder
            .Property(tipo => tipo.Orientacoes)
            .HasMaxLength(TipoResiduo.OrientacoesMaxLength);

        builder
            .HasIndex(tipo => tipo.Codigo)
            .IsUnique()
            .HasDatabaseName("UX_TipoResiduo_Codigo");
    }
}
