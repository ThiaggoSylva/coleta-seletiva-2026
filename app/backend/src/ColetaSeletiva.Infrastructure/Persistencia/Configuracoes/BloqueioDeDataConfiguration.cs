using ColetaSeletiva.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class BloqueioDeDataConfiguration : IEntityTypeConfiguration<BloqueioDeData>
{
    public void Configure(EntityTypeBuilder<BloqueioDeData> builder)
    {
        builder.ToTable("BloqueioData");
        builder.HasKey(bloqueio => bloqueio.Id);
        builder.Property(bloqueio => bloqueio.Id).ValueGeneratedNever();
        builder.Property(bloqueio => bloqueio.OrganizacaoId).IsRequired();
        builder.Property(bloqueio => bloqueio.DataLocal).HasColumnType("date").IsRequired();
        builder
            .Property(bloqueio => bloqueio.Motivo)
            .HasMaxLength(BloqueioDeData.MotivoMaxLength);
        builder.Property(bloqueio => bloqueio.CriadoPorUsuarioId).IsRequired();
        builder.Property(bloqueio => bloqueio.CriadoEmUtc).IsRequired();

        builder
            .HasIndex(bloqueio => new { bloqueio.OrganizacaoId, bloqueio.DataLocal })
            .IsUnique()
            .HasDatabaseName("UX_BloqueioData_OrganizacaoId_DataLocal");

        builder
            .HasOne<Organizacao>()
            .WithMany()
            .HasForeignKey(bloqueio => bloqueio.OrganizacaoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
