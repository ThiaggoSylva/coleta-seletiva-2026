using ColetaSeletiva.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class DisponibilidadeDeRetiradaConfiguration
    : IEntityTypeConfiguration<DisponibilidadeDeRetirada>
{
    public void Configure(EntityTypeBuilder<DisponibilidadeDeRetirada> builder)
    {
        builder.ToTable("DisponibilidadeRetirada");
        builder.HasKey(disponibilidade => disponibilidade.Id);
        builder.Property(disponibilidade => disponibilidade.Id).ValueGeneratedNever();
        builder.Property(disponibilidade => disponibilidade.OrganizacaoId).IsRequired();
        builder
            .Property(disponibilidade => disponibilidade.DiaDaSemana)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder
            .Property(disponibilidade => disponibilidade.Periodo)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(disponibilidade => disponibilidade.Capacidade).IsRequired();
        builder.Property(disponibilidade => disponibilidade.CriadoEmUtc).IsRequired();
        builder.Property(disponibilidade => disponibilidade.AtualizadoEmUtc).IsRequired();

        builder
            .HasIndex(disponibilidade => new
            {
                disponibilidade.OrganizacaoId,
                disponibilidade.DiaDaSemana,
                disponibilidade.Periodo,
            })
            .IsUnique()
            .HasDatabaseName("UX_DisponibilidadeRetirada_Organizacao_Dia_Periodo");

        builder
            .HasOne<Organizacao>()
            .WithMany()
            .HasForeignKey(disponibilidade => disponibilidade.OrganizacaoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
