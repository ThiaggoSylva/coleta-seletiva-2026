namespace ColetaSeletiva.Domain.Entidades;

/// <summary>
/// Tipo de resíduo habilitado por uma organização, com a indicação de retirada domiciliar (RF-07).
/// </summary>
public sealed class OrganizacaoTipo : IEntidadeDaOrganizacao
{
    private OrganizacaoTipo()
    {
    }

    private OrganizacaoTipo(
        Guid id,
        Guid tipoResiduoId,
        bool aceitaRetiradaDomiciliar,
        DateTimeOffset agora)
    {
        Id = id;
        TipoResiduoId = tipoResiduoId;
        AceitaRetiradaDomiciliar = aceitaRetiradaDomiciliar;
        CriadoEmUtc = agora.ToUniversalTime();
        AtualizadoEmUtc = CriadoEmUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrganizacaoId { get; private set; }

    public Guid TipoResiduoId { get; private set; }

    public bool AceitaRetiradaDomiciliar { get; private set; }

    public DateTimeOffset CriadoEmUtc { get; private set; }

    public DateTimeOffset AtualizadoEmUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    Guid? IEntidadeDaOrganizacao.OrganizacaoId
    {
        get => OrganizacaoId == Guid.Empty ? null : OrganizacaoId;
        set => OrganizacaoId = value
            ?? throw new ArgumentNullException(nameof(value), "A organização do vínculo é obrigatória.");
    }

    public static OrganizacaoTipo Criar(
        Guid tipoResiduoId,
        bool aceitaRetiradaDomiciliar,
        DateTimeOffset agora)
    {
        if (tipoResiduoId == Guid.Empty)
        {
            throw new ArgumentException("O tipo de resíduo é obrigatório.", nameof(tipoResiduoId));
        }

        return new OrganizacaoTipo(Guid.NewGuid(), tipoResiduoId, aceitaRetiradaDomiciliar, agora);
    }

    public void Atualizar(bool aceitaRetiradaDomiciliar, DateTimeOffset agora)
    {
        AceitaRetiradaDomiciliar = aceitaRetiradaDomiciliar;
        AtualizadoEmUtc = agora.ToUniversalTime();
    }
}
