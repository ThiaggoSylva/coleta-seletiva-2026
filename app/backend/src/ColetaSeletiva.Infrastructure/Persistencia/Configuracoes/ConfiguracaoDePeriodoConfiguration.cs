using ColetaSeletiva.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class ConfiguracaoDePeriodoConfiguration : IEntityTypeConfiguration<ConfiguracaoDePeriodo>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoDePeriodo> builder)
    {
        builder.ToTable("ConfiguracaoDePeriodo");
        builder.HasKey(periodo => periodo.Id);
        builder.Property(periodo => periodo.Id).ValueGeneratedNever();
        builder.Property(periodo => periodo.OrganizacaoId).IsRequired();
        builder
            .Property(periodo => periodo.Periodo)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(periodo => periodo.HoraInicio).HasColumnType("time").IsRequired();
        builder.Property(periodo => periodo.HoraFim).HasColumnType("time").IsRequired();
        builder.Property(periodo => periodo.CriadoEmUtc).IsRequired();
        builder.Property(periodo => periodo.AtualizadoEmUtc).IsRequired();

        builder
            .HasIndex(periodo => new { periodo.OrganizacaoId, periodo.Periodo })
            .IsUnique()
            .HasDatabaseName("UX_ConfiguracaoDePeriodo_OrganizacaoId_Periodo");

        builder
            .HasOne<Organizacao>()
            .WithMany()
            .HasForeignKey(periodo => periodo.OrganizacaoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
