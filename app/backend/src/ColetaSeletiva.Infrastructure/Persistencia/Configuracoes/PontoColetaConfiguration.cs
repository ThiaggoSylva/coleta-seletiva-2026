using ColetaSeletiva.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class PontoColetaConfiguration : IEntityTypeConfiguration<PontoDeColeta>
{
    public void Configure(EntityTypeBuilder<PontoDeColeta> builder)
    {
        builder.ToTable("PontoColeta");
        builder.HasKey(ponto => ponto.Id);
        builder.Property(ponto => ponto.Id).ValueGeneratedNever();
        builder.Property(ponto => ponto.OrganizacaoId).IsRequired();
        builder
            .Property(ponto => ponto.Nome)
            .HasMaxLength(PontoDeColeta.NomeMaxLength)
            .IsRequired();
        builder.Property(ponto => ponto.MunicipioId).IsRequired();
        builder
            .Property(ponto => ponto.Uf)
            .HasMaxLength(PontoDeColeta.UfLength)
            .IsRequired();
        builder
            .Property(ponto => ponto.Bairro)
            .HasMaxLength(PontoDeColeta.BairroMaxLength)
            .IsRequired();
        builder
            .Property(ponto => ponto.Rua)
            .HasMaxLength(PontoDeColeta.RuaMaxLength)
            .IsRequired();
        builder
            .Property(ponto => ponto.Numero)
            .HasMaxLength(PontoDeColeta.NumeroMaxLength)
            .IsRequired();
        builder
            .Property(ponto => ponto.Complemento)
            .HasMaxLength(PontoDeColeta.ComplementoMaxLength);
        builder
            .Property(ponto => ponto.Cep)
            .HasMaxLength(PontoDeColeta.CepLength)
            .IsRequired();
        builder
            .Property(ponto => ponto.Localizacao)
            .HasColumnType("geography")
            .IsRequired();
        builder
            .Property(ponto => ponto.Observacoes)
            .HasMaxLength(PontoDeColeta.ObservacoesMaxLength);
        builder
            .Property(ponto => ponto.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(ponto => ponto.ExcluidoEmUtc);
        builder.Property(ponto => ponto.CriadoEmUtc).IsRequired();
        builder.Property(ponto => ponto.AtualizadoEmUtc).IsRequired();
        builder.Property(ponto => ponto.RowVersion).IsRowVersion();

        builder
            .HasAlternateKey(ponto => new { ponto.OrganizacaoId, ponto.Id })
            .HasName("AK_PontoColeta_OrganizacaoId_Id");

        builder
            .HasIndex(ponto => new { ponto.OrganizacaoId, ponto.Nome })
            .IsUnique()
            .HasDatabaseName("UX_PontoColeta_OrganizacaoId_Nome");

        builder
            .HasIndex(ponto => new { ponto.OrganizacaoId, ponto.MunicipioId })
            .HasDatabaseName("IX_PontoColeta_OrganizacaoId_MunicipioId");

        // O índice espacial é criado por SQL na migration, usando GEOGRAPHY_AUTO_GRID.

        builder
            .HasOne<Organizacao>()
            .WithMany()
            .HasForeignKey(ponto => ponto.OrganizacaoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<OrganizacaoMunicipio>()
            .WithMany()
            .HasForeignKey(ponto => new { ponto.OrganizacaoId, ponto.MunicipioId })
            .HasPrincipalKey(vinculo => new { vinculo.OrganizacaoId, vinculo.MunicipioId })
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(ponto => ponto.Horarios)
            .WithOne()
            .HasForeignKey(horario => new { horario.OrganizacaoId, horario.PontoColetaId })
            .HasPrincipalKey(ponto => new { ponto.OrganizacaoId, ponto.Id })
            .OnDelete(DeleteBehavior.ClientCascade);

        builder
            .HasMany(ponto => ponto.Tipos)
            .WithOne()
            .HasForeignKey(tipo => new { tipo.OrganizacaoId, tipo.PontoColetaId })
            .HasPrincipalKey(ponto => new { ponto.OrganizacaoId, ponto.Id })
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
