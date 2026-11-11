namespace ColetaSeletiva.Domain.Entidades;

public sealed class Auditoria
{
    public const int AcaoMaxLength = 100;
    public const int EntidadeMaxLength = 100;
    public const int CamposAlteradosMaxLength = 2000;

    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? OrganizacaoId { get; set; }

    public Guid? AtorUsuarioId { get; set; }

    public string Acao { get; set; } = string.Empty;

    public string Entidade { get; set; } = string.Empty;

    public Guid? EntidadeId { get; set; }

    public Guid CorrelacaoId { get; set; }

    public DateTimeOffset OcorridoEmUtc { get; set; }

    public string? CamposAlterados { get; set; }
}
