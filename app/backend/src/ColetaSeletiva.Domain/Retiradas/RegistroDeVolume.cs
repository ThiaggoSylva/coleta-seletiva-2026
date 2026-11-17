using ColetaSeletiva.Domain.Entidades;

namespace ColetaSeletiva.Domain.Retiradas;

/// <summary>
/// Origem de um registro de volume (RF-15, RF-16).
/// </summary>
public enum OrigemDoVolume
{
    Retirada,
    RecebimentoEmPonto,
    Ajuste,
}

/// <summary>
/// Registro imutável de volume coletado, nunca editado nem apagado (RF-15, RF-16; RN-07).
/// </summary>
public sealed class RegistroDeVolume
{
    public const int OperadorNomeMaxLength = 150;
    public const int MotivoMaxLength = 500;

    private RegistroDeVolume()
    {
    }

    private RegistroDeVolume(
        Guid id,
        Guid organizacaoId,
        Guid tipoResiduoId,
        decimal quantidade,
        OrigemDoVolume origem,
        Guid? solicitacaoId,
        Guid? pontoId,
        Guid? registroOriginalId,
        Guid operadorUsuarioId,
        string operadorNome,
        DateTimeOffset ocorridoEmUtc,
        DateOnly dataLocal,
        string? motivo)
    {
        Id = id;
        OrganizacaoId = organizacaoId;
        TipoResiduoId = tipoResiduoId;
        Quantidade = quantidade;
        Origem = origem;
        SolicitacaoId = solicitacaoId;
        PontoId = pontoId;
        RegistroOriginalId = registroOriginalId;
        OperadorUsuarioId = operadorUsuarioId;
        OperadorNome = operadorNome;
        OcorridoEmUtc = ocorridoEmUtc.ToUniversalTime();
        DataLocal = dataLocal;
        Motivo = motivo;
        CriadoEmUtc = OcorridoEmUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrganizacaoId { get; private set; }

    public Guid TipoResiduoId { get; private set; }

    /// <summary>Quantidade com três casas; negativa apenas em ajuste.</summary>
    public decimal Quantidade { get; private set; }

    public OrigemDoVolume Origem { get; private set; }

    public Guid? SolicitacaoId { get; private set; }

    public Guid? PontoId { get; private set; }

    public Guid? RegistroOriginalId { get; private set; }

    public Guid OperadorUsuarioId { get; private set; }

    public string OperadorNome { get; private set; } = string.Empty;

    public DateTimeOffset OcorridoEmUtc { get; private set; }

    public DateOnly DataLocal { get; private set; }

    public string? Motivo { get; private set; }

    public DateTimeOffset CriadoEmUtc { get; private set; }

    public bool EhAjuste => Origem == OrigemDoVolume.Ajuste;

    public static RegistroDeVolume CriarRetirada(
        Guid organizacaoId,
        Guid tipoResiduoId,
        decimal quantidade,
        Guid solicitacaoId,
        Guid operadorUsuarioId,
        string operadorNome,
        DateTimeOffset ocorridoEmUtc,
        DateOnly dataLocal)
    {
        ValidarComum(organizacaoId, tipoResiduoId, operadorUsuarioId, operadorNome);

        if (!SolicitacaoItem.QuantidadeValida(quantidade) || quantidade <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantidade),
                quantidade,
                "A quantidade deve ser positiva, com até três casas decimais.");
        }

        if (solicitacaoId == Guid.Empty)
        {
            throw new ArgumentException("A solicitação é obrigatória.", nameof(solicitacaoId));
        }

        return new RegistroDeVolume(
            Guid.NewGuid(),
            organizacaoId,
            tipoResiduoId,
            quantidade,
            OrigemDoVolume.Retirada,
            solicitacaoId,
            pontoId: null,
            registroOriginalId: null,
            operadorUsuarioId,
            operadorNome.Trim(),
            ocorridoEmUtc,
            dataLocal,
            motivo: null);
    }

    public static RegistroDeVolume CriarRecebimento(
        Guid organizacaoId,
        Guid tipoResiduoId,
        decimal quantidade,
        Guid pontoId,
        Guid operadorUsuarioId,
        string operadorNome,
        DateTimeOffset ocorridoEmUtc,
        DateOnly dataLocal)
    {
        ValidarComum(organizacaoId, tipoResiduoId, operadorUsuarioId, operadorNome);

        if (!SolicitacaoItem.QuantidadeValida(quantidade) || quantidade <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantidade),
                quantidade,
                "A quantidade deve ser positiva, com até três casas decimais.");
        }

        if (pontoId == Guid.Empty)
        {
            throw new ArgumentException("O ponto é obrigatório.", nameof(pontoId));
        }

        return new RegistroDeVolume(
            Guid.NewGuid(),
            organizacaoId,
            tipoResiduoId,
            quantidade,
            OrigemDoVolume.RecebimentoEmPonto,
            solicitacaoId: null,
            pontoId,
            registroOriginalId: null,
            operadorUsuarioId,
            operadorNome.Trim(),
            ocorridoEmUtc,
            dataLocal,
            motivo: null);
    }

    public static RegistroDeVolume CriarAjuste(
        Guid organizacaoId,
        Guid tipoResiduoId,
        decimal quantidade,
        Guid registroOriginalId,
        Guid operadorUsuarioId,
        string operadorNome,
        DateTimeOffset ocorridoEmUtc,
        DateOnly dataLocal,
        string motivo)
    {
        ValidarComum(organizacaoId, tipoResiduoId, operadorUsuarioId, operadorNome);

        if (!SolicitacaoItem.QuantidadeValida(quantidade) || quantidade == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantidade),
                quantidade,
                "O ajuste deve ter quantidade diferente de zero, com até três casas decimais.");
        }

        if (registroOriginalId == Guid.Empty)
        {
            throw new ArgumentException(
                "O registro original é obrigatório.",
                nameof(registroOriginalId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        var motivoNormalizado = motivo.Trim();
        if (motivoNormalizado.Length > MotivoMaxLength)
        {
            throw new ArgumentException(
                $"O motivo deve ter no máximo {MotivoMaxLength} caracteres.",
                nameof(motivo));
        }

        return new RegistroDeVolume(
            Guid.NewGuid(),
            organizacaoId,
            tipoResiduoId,
            quantidade,
            OrigemDoVolume.Ajuste,
            solicitacaoId: null,
            pontoId: null,
            registroOriginalId,
            operadorUsuarioId,
            operadorNome.Trim(),
            ocorridoEmUtc,
            dataLocal,
            motivoNormalizado);
    }

    private static void ValidarComum(
        Guid organizacaoId,
        Guid tipoResiduoId,
        Guid operadorUsuarioId,
        string operadorNome)
    {
        if (organizacaoId == Guid.Empty)
        {
            throw new ArgumentException("A organização é obrigatória.", nameof(organizacaoId));
        }

        if (tipoResiduoId == Guid.Empty)
        {
            throw new ArgumentException("O tipo de resíduo é obrigatório.", nameof(tipoResiduoId));
        }

        if (operadorUsuarioId == Guid.Empty)
        {
            throw new ArgumentException("O operador é obrigatório.", nameof(operadorUsuarioId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(operadorNome);

        if (operadorNome.Trim().Length > OperadorNomeMaxLength)
        {
            throw new ArgumentException(
                $"O nome do operador deve ter no máximo {OperadorNomeMaxLength} caracteres.",
                nameof(operadorNome));
        }
    }
}
