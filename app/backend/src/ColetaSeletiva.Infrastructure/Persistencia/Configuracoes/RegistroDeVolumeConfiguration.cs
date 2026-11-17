using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Retiradas;
using ColetaSeletiva.Infrastructure.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class RegistroDeVolumeConfiguration : IEntityTypeConfiguration<RegistroDeVolume>
{
    public void Configure(EntityTypeBuilder<RegistroDeVolume> builder)
    {
        builder.ToTable("RegistroVolume");
        builder.HasKey(volume => volume.Id);
        builder.Property(volume => volume.Id).ValueGeneratedNever();
        builder.Property(volume => volume.OrganizacaoId).IsRequired();
        builder.Property(volume => volume.TipoResiduoId).IsRequired();
        builder.Property(volume => volume.Quantidade).HasPrecision(12, 3).IsRequired();
        builder
            .Property(volume => volume.Origem)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(volume => volume.SolicitacaoId);
        builder.Property(volume => volume.PontoId);
        builder.Property(volume => volume.RegistroOriginalId);
        builder.Property(volume => volume.OperadorUsuarioId).IsRequired();
        builder
            .Property(volume => volume.OperadorNome)
            .HasMaxLength(RegistroDeVolume.OperadorNomeMaxLength)
            .IsRequired();
        builder.Property(volume => volume.OcorridoEmUtc).IsRequired();
        builder.Property(volume => volume.DataLocal).HasColumnType("date").IsRequired();
        builder
            .Property(volume => volume.Motivo)
            .HasMaxLength(RegistroDeVolume.MotivoMaxLength);
        builder.Property(volume => volume.CriadoEmUtc).IsRequired();

        builder
            .HasIndex(volume => new { volume.OrganizacaoId, volume.DataLocal, volume.TipoResiduoId })
            .HasDatabaseName("IX_RegistroVolume_Organizacao_Data_Tipo");

        builder
            .HasIndex(volume => new { volume.OrganizacaoId, volume.Origem })
            .HasDatabaseName("IX_RegistroVolume_Organizacao_Origem");

        builder
            .HasIndex(volume => volume.RegistroOriginalId)
            .HasDatabaseName("IX_RegistroVolume_RegistroOriginalId");

        builder
            .HasOne<Organizacao>()
            .WithMany()
            .HasForeignKey(volume => volume.OrganizacaoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<TipoResiduo>()
            .WithMany()
            .HasForeignKey(volume => volume.TipoResiduoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<PontoDeColeta>()
            .WithMany()
            .HasForeignKey(volume => volume.PontoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<SolicitacaoDeRetirada>()
            .WithMany()
            .HasForeignKey(volume => volume.SolicitacaoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<RegistroDeVolume>()
            .WithMany()
            .HasForeignKey(volume => volume.RegistroOriginalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(volume => volume.OperadorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
