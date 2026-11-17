using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Retiradas;
using ColetaSeletiva.Infrastructure.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class SolicitacaoDeRetiradaConfiguration
    : IEntityTypeConfiguration<SolicitacaoDeRetirada>
{
    public void Configure(EntityTypeBuilder<SolicitacaoDeRetirada> builder)
    {
        builder.ToTable("SolicitacaoRetirada");
        builder.HasKey(solicitacao => solicitacao.Id);
        builder.Property(solicitacao => solicitacao.Id).ValueGeneratedNever();
        builder.Property(solicitacao => solicitacao.OrganizacaoId).IsRequired();
        builder.Property(solicitacao => solicitacao.CidadaoId).IsRequired();
        builder
            .Property(solicitacao => solicitacao.NomeCidadao)
            .HasMaxLength(SolicitacaoDeRetirada.NomeMaxLength)
            .IsRequired();
        builder
            .Property(solicitacao => solicitacao.TelefoneCidadao)
            .HasMaxLength(SolicitacaoDeRetirada.TelefoneMaxLength);
        builder
            .Property(solicitacao => solicitacao.Uf)
            .HasMaxLength(SolicitacaoDeRetirada.UfLength)
            .IsRequired();
        builder.Property(solicitacao => solicitacao.MunicipioId).IsRequired();
        builder
            .Property(solicitacao => solicitacao.MunicipioNome)
            .HasMaxLength(SolicitacaoDeRetirada.MunicipioNomeMaxLength)
            .IsRequired();
        builder
            .Property(solicitacao => solicitacao.Bairro)
            .HasMaxLength(SolicitacaoDeRetirada.BairroMaxLength)
            .IsRequired();
        builder
            .Property(solicitacao => solicitacao.Rua)
            .HasMaxLength(SolicitacaoDeRetirada.RuaMaxLength)
            .IsRequired();
        builder
            .Property(solicitacao => solicitacao.Numero)
            .HasMaxLength(SolicitacaoDeRetirada.NumeroMaxLength)
            .IsRequired();
        builder
            .Property(solicitacao => solicitacao.Complemento)
            .HasMaxLength(SolicitacaoDeRetirada.ComplementoMaxLength);
        builder
            .Property(solicitacao => solicitacao.Cep)
            .HasMaxLength(SolicitacaoDeRetirada.CepLength)
            .IsRequired();
        builder
            .Property(solicitacao => solicitacao.PontoReferencia)
            .HasMaxLength(SolicitacaoDeRetirada.ReferenciaMaxLength);
        builder
            .Property(solicitacao => solicitacao.Observacoes)
            .HasMaxLength(SolicitacaoDeRetirada.ObservacoesMaxLength);
        builder.Property(solicitacao => solicitacao.DataLocal).HasColumnType("date").IsRequired();
        builder
            .Property(solicitacao => solicitacao.Periodo)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder
            .Property(solicitacao => solicitacao.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder
            .Property(solicitacao => solicitacao.Motivo)
            .HasMaxLength(SolicitacaoDeRetirada.MotivoMaxLength);
        builder.Property(solicitacao => solicitacao.CriadoEmUtc).IsRequired();
        builder.Property(solicitacao => solicitacao.AtualizadoEmUtc).IsRequired();
        builder.Property(solicitacao => solicitacao.RowVersion).IsRowVersion();

        builder
            .HasAlternateKey(solicitacao => new { solicitacao.OrganizacaoId, solicitacao.Id })
            .HasName("AK_SolicitacaoRetirada_OrganizacaoId_Id");

        builder
            .HasIndex(solicitacao => new
            {
                solicitacao.OrganizacaoId,
                solicitacao.Estado,
                solicitacao.DataLocal,
            })
            .HasDatabaseName("IX_SolicitacaoRetirada_Organizacao_Estado_Data");

        builder
            .HasIndex(solicitacao => new
            {
                solicitacao.OrganizacaoId,
                solicitacao.DataLocal,
                solicitacao.Periodo,
                solicitacao.Estado,
            })
            .HasDatabaseName("IX_SolicitacaoRetirada_Organizacao_Data_Periodo_Estado");

        builder
            .HasIndex(solicitacao => new
            {
                solicitacao.CidadaoId,
                solicitacao.OrganizacaoId,
                solicitacao.Estado,
            })
            .HasDatabaseName("IX_SolicitacaoRetirada_Cidadao_Organizacao_Estado");

        builder
            .HasOne<Organizacao>()
            .WithMany()
            .HasForeignKey(solicitacao => solicitacao.OrganizacaoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(solicitacao => solicitacao.CidadaoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(solicitacao => solicitacao.Itens)
            .WithOne()
            .HasForeignKey(item => item.SolicitacaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(solicitacao => solicitacao.Historico)
            .WithOne()
            .HasForeignKey(historico => historico.SolicitacaoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SolicitacaoItemConfiguration : IEntityTypeConfiguration<SolicitacaoItem>
{
    public void Configure(EntityTypeBuilder<SolicitacaoItem> builder)
    {
        builder.ToTable("SolicitacaoItem");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.SolicitacaoId).IsRequired();
        builder.Property(item => item.TipoResiduoId).IsRequired();
        builder
            .Property(item => item.QuantidadeEstimada)
            .HasPrecision(12, 3)
            .IsRequired();

        builder
            .HasIndex(item => new { item.SolicitacaoId, item.TipoResiduoId })
            .IsUnique()
            .HasDatabaseName("UX_SolicitacaoItem_SolicitacaoId_TipoResiduoId");

        builder
            .HasOne<TipoResiduo>()
            .WithMany()
            .HasForeignKey(item => item.TipoResiduoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SolicitacaoHistoricoConfiguration
    : IEntityTypeConfiguration<SolicitacaoHistorico>
{
    public void Configure(EntityTypeBuilder<SolicitacaoHistorico> builder)
    {
        builder.ToTable("SolicitacaoHistorico");
        builder.HasKey(historico => historico.Id);
        builder.Property(historico => historico.Id).ValueGeneratedNever();
        builder.Property(historico => historico.SolicitacaoId).IsRequired();
        builder
            .Property(historico => historico.EstadoAnterior)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder
            .Property(historico => historico.EstadoNovo)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(historico => historico.AtorUsuarioId).IsRequired();
        builder.Property(historico => historico.OcorridoEmUtc).IsRequired();
        builder
            .Property(historico => historico.Motivo)
            .HasMaxLength(SolicitacaoDeRetirada.MotivoMaxLength);

        builder
            .HasIndex(historico => new { historico.SolicitacaoId, historico.OcorridoEmUtc })
            .HasDatabaseName("IX_SolicitacaoHistorico_SolicitacaoId_Ocorrido");
    }
}
