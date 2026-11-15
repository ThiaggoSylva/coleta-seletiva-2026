using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Identidade;
using Microsoft.AspNetCore.Identity;

namespace ColetaSeletiva.Infrastructure.Identidade;

public sealed class Usuario : IdentityUser<Guid>, IEntidadeDaOrganizacao
{
    public const int NomeMaxLength = LimitesDeConta.NomeMaxLength;
    public const int TelefoneMaxLength = LimitesDeConta.TelefoneMaxLength;

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
