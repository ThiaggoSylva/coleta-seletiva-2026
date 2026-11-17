using ColetaSeletiva.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class JanelaDeRetiradaConfiguration : IEntityTypeConfiguration<JanelaDeRetirada>
{
    public void Configure(EntityTypeBuilder<JanelaDeRetirada> builder)
    {
        builder.ToTable("JanelaRetirada");
        builder.HasKey(janela => janela.Id);
        builder.Property(janela => janela.Id).ValueGeneratedNever();
        builder.Property(janela => janela.OrganizacaoId).IsRequired();
        builder.Property(janela => janela.DataLocal).HasColumnType("date").IsRequired();
        builder
            .Property(janela => janela.Periodo)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(janela => janela.CriadoEmUtc).IsRequired();

        builder
            .HasIndex(janela => new { janela.OrganizacaoId, janela.DataLocal, janela.Periodo })
            .IsUnique()
            .HasDatabaseName("UX_JanelaRetirada_Organizacao_Data_Periodo");

        builder
            .HasOne<Organizacao>()
            .WithMany()
            .HasForeignKey(janela => janela.OrganizacaoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
