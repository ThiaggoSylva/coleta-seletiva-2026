namespace ColetaSeletiva.Domain.Entidades;

/// <summary>
/// Janela de retirada criada sob demanda para ancorar a ocupação (RF-10; RN-02).
/// </summary>
public sealed class JanelaDeRetirada
{
    private JanelaDeRetirada()
    {
    }

    private JanelaDeRetirada(
        Guid id,
        Guid organizacaoId,
        DateOnly dataLocal,
        PeriodoDeRetirada periodo,
        DateTimeOffset agora)
    {
        Id = id;
        OrganizacaoId = organizacaoId;
        DataLocal = dataLocal;
        Periodo = periodo;
        CriadoEmUtc = agora.ToUniversalTime();
    }

    public Guid Id { get; private set; }

    public Guid OrganizacaoId { get; private set; }

    public DateOnly DataLocal { get; private set; }

    public PeriodoDeRetirada Periodo { get; private set; }

    public DateTimeOffset CriadoEmUtc { get; private set; }

    public static JanelaDeRetirada Criar(
        Guid organizacaoId,
        DateOnly dataLocal,
        PeriodoDeRetirada periodo,
        DateTimeOffset agora)
    {
        if (organizacaoId == Guid.Empty)
        {
            throw new ArgumentException(
                "A organização da janela é obrigatória.",
                nameof(organizacaoId));
        }

        if (!Enum.IsDefined(periodo))
        {
            throw new ArgumentOutOfRangeException(nameof(periodo), periodo, "Período inválido.");
        }

        return new JanelaDeRetirada(Guid.NewGuid(), organizacaoId, dataLocal, periodo, agora);
    }
}
