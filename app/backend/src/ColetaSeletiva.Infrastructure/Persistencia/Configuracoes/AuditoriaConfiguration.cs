using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class AuditoriaConfiguration : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> builder)
    {
        builder.ToTable("Auditoria");
        builder.HasKey(auditoria => auditoria.Id);
        builder.Property(auditoria => auditoria.Id).ValueGeneratedNever();
        builder
            .Property(auditoria => auditoria.Acao)
            .HasMaxLength(Auditoria.AcaoMaxLength)
            .IsRequired();
        builder
            .Property(auditoria => auditoria.Entidade)
            .HasMaxLength(Auditoria.EntidadeMaxLength)
            .IsRequired();
        builder
            .Property(auditoria => auditoria.CamposAlterados)
            .HasMaxLength(Auditoria.CamposAlteradosMaxLength);
        builder.Property(auditoria => auditoria.CorrelacaoId).IsRequired();
        builder.Property(auditoria => auditoria.OcorridoEmUtc).IsRequired();

        builder
            .HasIndex(auditoria => new { auditoria.OrganizacaoId, auditoria.OcorridoEmUtc })
            .HasDatabaseName("IX_Auditoria_OrganizacaoId_OcorridoEmUtc");
        builder
            .HasIndex(auditoria => auditoria.CorrelacaoId)
            .HasDatabaseName("IX_Auditoria_CorrelacaoId");
        builder
            .HasIndex(auditoria => new { auditoria.AtorUsuarioId, auditoria.OcorridoEmUtc })
            .HasDatabaseName("IX_Auditoria_AtorUsuarioId_OcorridoEmUtc");

        builder
            .HasOne<Organizacao>()
            .WithMany()
            .HasForeignKey(auditoria => auditoria.OrganizacaoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(auditoria => auditoria.AtorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
