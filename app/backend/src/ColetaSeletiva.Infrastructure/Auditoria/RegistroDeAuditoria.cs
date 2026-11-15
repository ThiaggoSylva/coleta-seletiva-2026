using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure.Persistencia;

namespace ColetaSeletiva.Infrastructure.AuditoriaDeEventos;

internal sealed class RegistroDeAuditoria : IRegistroDeAuditoria
{
    private readonly ColetaSeletivaDbContext _contexto;

    public RegistroDeAuditoria(ColetaSeletivaDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task RegistrarAsync(
        EventoDeAuditoria evento,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evento);

        _contexto.Auditorias.Add(new Auditoria
        {
            OrganizacaoId = evento.OrganizacaoId,
            AtorUsuarioId = evento.AtorUsuarioId,
            Acao = evento.Acao,
            Entidade = evento.Entidade,
            EntidadeId = evento.EntidadeId,
            CorrelacaoId = evento.CorrelacaoId,
            OcorridoEmUtc = evento.OcorridoEmUtc,
            CamposAlterados = evento.CamposAlterados,
        });

        await _contexto.SaveChangesAsync(cancellationToken);
    }
}
