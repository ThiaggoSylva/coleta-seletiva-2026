namespace ColetaSeletiva.Domain.Entidades;

/// <summary>
/// Tipo de resíduo aceito por um ponto de coleta (RF-08).
/// </summary>
public sealed class PontoTipo : IEntidadeDaOrganizacao
{
    private PontoTipo()
    {
    }

    private PontoTipo(Guid id, Guid pontoColetaId, Guid tipoResiduoId)
    {
        Id = id;
        PontoColetaId = pontoColetaId;
        TipoResiduoId = tipoResiduoId;
    }

    public Guid Id { get; private set; }

    public Guid OrganizacaoId { get; private set; }

    public Guid PontoColetaId { get; private set; }

    public Guid TipoResiduoId { get; private set; }

    Guid? IEntidadeDaOrganizacao.OrganizacaoId
    {
        get => OrganizacaoId == Guid.Empty ? null : OrganizacaoId;
        set => OrganizacaoId = value
            ?? throw new ArgumentNullException(nameof(value), "A organização do tipo é obrigatória.");
    }

    internal static PontoTipo Criar(Guid pontoColetaId, Guid tipoResiduoId)
    {
        if (pontoColetaId == Guid.Empty)
        {
            throw new ArgumentException("O ponto do tipo é obrigatório.", nameof(pontoColetaId));
        }

        if (tipoResiduoId == Guid.Empty)
        {
            throw new ArgumentException("O tipo de resíduo é obrigatório.", nameof(tipoResiduoId));
        }

        return new PontoTipo(Guid.NewGuid(), pontoColetaId, tipoResiduoId);
    }
}
