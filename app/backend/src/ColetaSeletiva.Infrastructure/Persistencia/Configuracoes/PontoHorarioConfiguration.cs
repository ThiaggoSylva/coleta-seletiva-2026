using ColetaSeletiva.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class PontoHorarioConfiguration : IEntityTypeConfiguration<PontoHorario>
{
    public void Configure(EntityTypeBuilder<PontoHorario> builder)
    {
        builder.ToTable("PontoHorario");
        builder.HasKey(horario => horario.Id);
        builder.Property(horario => horario.Id).ValueGeneratedNever();
        builder.Property(horario => horario.OrganizacaoId).IsRequired();
        builder.Property(horario => horario.PontoColetaId).IsRequired();
        builder
            .Property(horario => horario.DiaDaSemana)
            .HasConversion<string>()
            .HasMaxLength(15)
            .IsRequired();
        builder.Property(horario => horario.HoraInicio).HasColumnType("time").IsRequired();
        builder.Property(horario => horario.HoraFim).HasColumnType("time").IsRequired();

        builder
            .HasIndex(horario => new { horario.OrganizacaoId, horario.PontoColetaId })
            .HasDatabaseName("IX_PontoHorario_OrganizacaoId_PontoColetaId");
    }
}
