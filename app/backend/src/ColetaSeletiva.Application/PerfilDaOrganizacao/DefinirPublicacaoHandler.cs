using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;

namespace ColetaSeletiva.Application.PerfilDaOrganizacao;

public sealed class DefinirPublicacaoHandler : ManipuladorDoPerfilDaOrganizacao
{
    private readonly IPerfilDaOrganizacao _perfil;

    public DefinirPublicacaoHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IPerfilDaOrganizacao perfil)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _perfil = perfil;
    }

    public Task<Result<PublicacaoDaOrganizacaoResposta>> ExecutarAsync(
        PublicacaoDaOrganizacaoEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        return ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.PublicacaoAlterada,
            "publicada",
            (organizacaoId, _) => _perfil.DefinirPublicacaoAsync(
                organizacaoId,
                entrada.Publicada,
                cancellationToken),
            cancellationToken);
    }
}
