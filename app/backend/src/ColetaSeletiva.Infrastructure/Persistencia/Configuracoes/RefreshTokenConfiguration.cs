using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshToken");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();
        builder.Property(token => token.UsuarioId).IsRequired();
        builder.Property(token => token.FamiliaId).IsRequired();
        builder
            .Property(token => token.Hash)
            .HasMaxLength(RefreshToken.HashMaxLength)
            .IsUnicode(false)
            .IsFixedLength()
            .IsRequired();
        builder.Property(token => token.CriadoEmUtc).IsRequired();
        builder.Property(token => token.ExpiraEmUtc).IsRequired();
        builder.Property(token => token.ConsumidoEmUtc);
        builder.Property(token => token.RevogadoEmUtc);

        builder
            .HasIndex(token => token.Hash)
            .IsUnique()
            .HasDatabaseName("UX_RefreshToken_Hash");
        builder
            .HasIndex(token => new { token.UsuarioId, token.FamiliaId })
            .HasDatabaseName("IX_RefreshToken_UsuarioId_FamiliaId");

        builder
            .HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(token => token.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
