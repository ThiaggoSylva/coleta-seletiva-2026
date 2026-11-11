using ColetaSeletiva.Infrastructure.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder
            .Property(usuario => usuario.Nome)
            .HasMaxLength(Usuario.NomeMaxLength)
            .IsRequired();
        builder
            .Property(usuario => usuario.Telefone)
            .HasMaxLength(Usuario.TelefoneMaxLength);
        builder.Property(usuario => usuario.Ativo).IsRequired();
        builder.Property(usuario => usuario.TrocaSenhaPendente).IsRequired();
        builder.Property(usuario => usuario.CriadoEmUtc).IsRequired();
        builder.Property(usuario => usuario.AtualizadoEmUtc).IsRequired();
        builder.Property(usuario => usuario.RowVersion).IsRowVersion();

        builder
            .HasIndex(usuario => usuario.NormalizedUserName)
            .IsUnique()
            .HasFilter("[NormalizedUserName] IS NOT NULL")
            .HasDatabaseName("UX_Usuario_NormalizedUserName");
        builder
            .HasIndex(usuario => usuario.NormalizedEmail)
            .IsUnique()
            .HasFilter("[NormalizedEmail] IS NOT NULL")
            .HasDatabaseName("UX_Usuario_NormalizedEmail");
        builder
            .HasIndex(usuario => new { usuario.OrganizacaoId, usuario.Id })
            .IsUnique()
            .HasFilter("[OrganizacaoId] IS NOT NULL")
            .HasDatabaseName("UX_Usuario_OrganizacaoId_Id");
        builder
            .HasIndex(usuario => usuario.OrganizacaoId)
            .HasDatabaseName("IX_Usuario_OrganizacaoId");

        builder
            .HasOne(usuario => usuario.Organizacao)
            .WithMany()
            .HasForeignKey(usuario => usuario.OrganizacaoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
