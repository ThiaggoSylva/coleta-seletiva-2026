using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColetaSeletiva.Infrastructure.Persistencia.Configuracoes;

internal sealed class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
{
    public void Configure(EntityTypeBuilder<Notificacao> builder)
    {
        builder.ToTable("Notificacao");
        builder.HasKey(notificacao => notificacao.Id);
        builder.Property(notificacao => notificacao.Id).ValueGeneratedNever();
        builder.Property(notificacao => notificacao.UsuarioId).IsRequired();
        builder.Property(notificacao => notificacao.OrganizacaoId);
        builder
            .Property(notificacao => notificacao.Tipo)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(notificacao => notificacao.ReferenciaId).IsRequired();
        builder
            .Property(notificacao => notificacao.Titulo)
            .HasMaxLength(Notificacao.TituloMaxLength)
            .IsRequired();
        builder
            .Property(notificacao => notificacao.Mensagem)
            .HasMaxLength(Notificacao.MensagemMaxLength)
            .IsRequired();
        builder.Property(notificacao => notificacao.Lida).IsRequired();
        builder.Property(notificacao => notificacao.LidaEmUtc);
        builder.Property(notificacao => notificacao.CriadaEmUtc).IsRequired();

        builder
            .HasIndex(notificacao => new { notificacao.UsuarioId, notificacao.Lida })
            .HasDatabaseName("IX_Notificacao_UsuarioId_Lida");

        builder
            .HasIndex(notificacao => new { notificacao.UsuarioId, notificacao.CriadaEmUtc })
            .HasDatabaseName("IX_Notificacao_UsuarioId_CriadaEmUtc");

        builder
            .HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(notificacao => notificacao.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<Organizacao>()
            .WithMany()
            .HasForeignKey(notificacao => notificacao.OrganizacaoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
