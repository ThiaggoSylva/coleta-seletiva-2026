using ColetaSeletiva.Domain.Entidades;
using Microsoft.AspNetCore.Identity;

namespace ColetaSeletiva.Infrastructure.Identidade;

public sealed class Usuario : IdentityUser<Guid>, IEntidadeDaOrganizacao
{
    public const int NomeMaxLength = 150;
    public const int TelefoneMaxLength = 20;

    public Usuario()
    {
        Id = Guid.NewGuid();
    }

    public Guid? OrganizacaoId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Telefone { get; set; }

    public bool Ativo { get; set; } = true;

    public bool TrocaSenhaPendente { get; set; }

    public DateTimeOffset CriadoEmUtc { get; set; }

    public DateTimeOffset AtualizadoEmUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Organizacao? Organizacao { get; set; }
}
