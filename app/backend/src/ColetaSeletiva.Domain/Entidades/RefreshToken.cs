namespace ColetaSeletiva.Domain.Entidades;

public sealed class RefreshToken
{
    public const int HashMaxLength = 64;

    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UsuarioId { get; set; }

    public Guid FamiliaId { get; set; }

    public string Hash { get; set; } = string.Empty;

    public DateTimeOffset CriadoEmUtc { get; set; }

    public DateTimeOffset ExpiraEmUtc { get; set; }

    public DateTimeOffset? ConsumidoEmUtc { get; set; }

    public DateTimeOffset? RevogadoEmUtc { get; set; }
}
