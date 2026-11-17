namespace ColetaSeletiva.Domain.Entidades;

/// <summary>
/// Data local bloqueada para novas solicitações de retirada (RF-10; RN-12).
/// </summary>
public sealed class BloqueioDeData : IEntidadeDaOrganizacao
{
    public const int MotivoMaxLength = 300;

    private BloqueioDeData()
    {
    }

    private BloqueioDeData(
        Guid id,
        DateOnly dataLocal,
        string? motivo,
        Guid criadoPorUsuarioId,
        DateTimeOffset agora)
    {
        Id = id;
        DataLocal = dataLocal;
        Motivo = NormalizarOpcional(motivo);
        CriadoPorUsuarioId = criadoPorUsuarioId;
        CriadoEmUtc = agora.ToUniversalTime();
    }

    public Guid Id { get; private set; }

    public Guid OrganizacaoId { get; private set; }

    public DateOnly DataLocal { get; private set; }

    public string? Motivo { get; private set; }

    public Guid CriadoPorUsuarioId { get; private set; }

    public DateTimeOffset CriadoEmUtc { get; private set; }

    Guid? IEntidadeDaOrganizacao.OrganizacaoId
    {
        get => OrganizacaoId == Guid.Empty ? null : OrganizacaoId;
        set => OrganizacaoId = value
            ?? throw new ArgumentNullException(nameof(value), "A organização do bloqueio é obrigatória.");
    }

    public static BloqueioDeData Criar(
        DateOnly dataLocal,
        string? motivo,
        Guid criadoPorUsuarioId,
        DateTimeOffset agora)
    {
        if (criadoPorUsuarioId == Guid.Empty)
        {
            throw new ArgumentException(
                "O autor do bloqueio é obrigatório.",
                nameof(criadoPorUsuarioId));
        }

        return new BloqueioDeData(Guid.NewGuid(), dataLocal, motivo, criadoPorUsuarioId, agora);
    }

    private static string? NormalizarOpcional(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var normalizado = valor.Trim();
        if (normalizado.Length > MotivoMaxLength)
        {
            throw new ArgumentException(
                $"O motivo deve ter no máximo {MotivoMaxLength} caracteres.",
                nameof(valor));
        }

        return normalizado;
    }
}
