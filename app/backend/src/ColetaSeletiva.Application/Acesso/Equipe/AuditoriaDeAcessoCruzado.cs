using ColetaSeletiva.Application.Auditoria;

namespace ColetaSeletiva.Application.Acesso.Equipe;

internal static class AuditoriaDeAcessoCruzado
{
    public static async Task<bool> RegistrarSeAplicavelAsync(
        IDeteccaoDeAcessoCruzado deteccao,
        IRegistroDeAuditoria auditoria,
        TimeProvider relogio,
        Guid organizacaoId,
        Guid atorUsuarioId,
        Guid alvoUsuarioId,
        Guid correlacaoId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deteccao);
        ArgumentNullException.ThrowIfNull(auditoria);
        ArgumentNullException.ThrowIfNull(relogio);

        if (!await deteccao.ExisteEmOutraOrganizacaoAsync(
                organizacaoId,
                alvoUsuarioId,
                cancellationToken))
        {
            return false;
        }

        await auditoria.RegistrarAsync(
            new EventoDeAuditoria(
                OrganizacaoId: organizacaoId,
                AtorUsuarioId: atorUsuarioId,
                Acao: AcoesAuditadas.AcessoCruzadoNegado,
                Entidade: EntidadesAuditadas.Usuario,
                EntidadeId: alvoUsuarioId,
                CorrelacaoId: correlacaoId,
                OcorridoEmUtc: relogio.GetUtcNow()),
            cancellationToken);

        return true;
    }
}
