namespace ColetaSeletiva.Application.Auditoria;

public sealed record EventoDeAuditoria(
    Guid? OrganizacaoId,
    Guid? AtorUsuarioId,
    string Acao,
    string Entidade,
    Guid? EntidadeId,
    Guid CorrelacaoId,
    DateTimeOffset OcorridoEmUtc,
    string? CamposAlterados = null);

public interface IRegistroDeAuditoria
{
    Task RegistrarAsync(EventoDeAuditoria evento, CancellationToken cancellationToken = default);
}
