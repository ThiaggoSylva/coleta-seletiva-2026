using ColetaSeletiva.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class OrganizacaoConfiguration : IEntityTypeConfiguration<Organizacao>
{
    public void Configure(EntityTypeBuilder<Organizacao> builder)
    {
        builder.ToTable("Organizacao");
        builder.HasKey(organizacao => organizacao.Id);
        builder.Property(organizacao => organizacao.Id).ValueGeneratedNever();
        builder
            .Property(organizacao => organizacao.Nome)
            .HasMaxLength(Organizacao.NomeMaxLength)
            .IsRequired();
        builder
            .Property(organizacao => organizacao.Tipo)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(organizacao => organizacao.Publicada).IsRequired();
        builder.Property(organizacao => organizacao.CriadoEmUtc).IsRequired();
        builder.Property(organizacao => organizacao.AtualizadoEmUtc).IsRequired();
        builder.Property(organizacao => organizacao.RowVersion).IsRowVersion();
    }
}
