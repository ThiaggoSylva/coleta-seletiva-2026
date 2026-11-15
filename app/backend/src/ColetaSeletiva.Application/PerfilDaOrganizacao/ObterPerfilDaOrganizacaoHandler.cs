using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;

namespace ColetaSeletiva.Application.PerfilDaOrganizacao;

public sealed class ObterPerfilDaOrganizacaoHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IPerfilDaOrganizacao _perfil;

    public ObterPerfilDaOrganizacaoHandler(
        ITenantContext tenantContext,
        IPerfilDaOrganizacao perfil)
    {
        _tenantContext = tenantContext;
        _perfil = perfil;
    }

    public async Task<Result<PerfilDaOrganizacaoResposta>> ExecutarAsync(
        CancellationToken cancellationToken = default)
    {
        var escopo = EscopoDaOrganizacaoHelper.Obter(_tenantContext);
        if (escopo.IsFailed)
        {
            return Result.Fail<PerfilDaOrganizacaoResposta>(escopo.Errors);
        }

        var perfil = await _perfil.ObterAsync(escopo.Value.OrganizacaoId, cancellationToken);

        return perfil is null
            ? Result.Fail<PerfilDaOrganizacaoResposta>(
                new ErroNaoEncontrado("Organização não encontrada."))
            : Result.Ok(perfil);
    }
}
