namespace ColetaSeletiva.Domain.Entidades;

/// <summary>
/// Município atendido por uma organização (RF-06, DT-15).
/// </summary>
public sealed class OrganizacaoMunicipio : IEntidadeDaOrganizacao
{
    private OrganizacaoMunicipio()
    {
    }

    private OrganizacaoMunicipio(Guid id, int municipioId, DateTimeOffset agora)
    {
        Id = id;
        MunicipioId = municipioId;
        CriadoEmUtc = agora.ToUniversalTime();
    }

    public Guid Id { get; private set; }

    public Guid OrganizacaoId { get; private set; }

    /// <summary>Código IBGE do município.</summary>
    public int MunicipioId { get; private set; }

    public DateTimeOffset CriadoEmUtc { get; private set; }

    Guid? IEntidadeDaOrganizacao.OrganizacaoId
    {
        get => OrganizacaoId == Guid.Empty ? null : OrganizacaoId;
        set => OrganizacaoId = value
            ?? throw new ArgumentNullException(nameof(value), "A organização do vínculo é obrigatória.");
    }

    public static OrganizacaoMunicipio Criar(int municipioId, DateTimeOffset agora)
    {
        if (municipioId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(municipioId),
                municipioId,
                "O município é obrigatório.");
        }

        return new OrganizacaoMunicipio(Guid.NewGuid(), municipioId, agora);
    }
}
