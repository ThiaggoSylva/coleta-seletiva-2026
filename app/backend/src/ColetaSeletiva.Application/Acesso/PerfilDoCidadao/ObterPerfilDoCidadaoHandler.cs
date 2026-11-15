using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;

namespace ColetaSeletiva.Application.Acesso.PerfilDoCidadao;

public sealed class ObterPerfilDoCidadaoHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IPerfilDeCidadao _perfis;

    public ObterPerfilDoCidadaoHandler(
        ITenantContext tenantContext,
        IPerfilDeCidadao perfis)
    {
        _tenantContext = tenantContext;
        _perfis = perfis;
    }

    public async Task<Result<PerfilDoCidadao>> ExecutarAsync(
        CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId.IsFailed)
        {
            return Result.Fail<PerfilDoCidadao>(usuarioId.Errors);
        }

        return await _perfis.ObterAsync(usuarioId.Value, cancellationToken);
    }

    private Result<Guid> ObterUsuarioId()
    {
        if (!_tenantContext.Autenticado)
        {
            return Result.Fail<Guid>(
                new ErroNaoAutenticado("É necessário autenticar para consultar o perfil."));
        }

        if (!_tenantContext.EhCidadao)
        {
            return Result.Fail<Guid>(
                new ErroDeAcessoNegado("O perfil é exclusivo do cidadão."));
        }

        if (!_tenantContext.EscopoValido || _tenantContext.UsuarioId is not Guid usuarioId)
        {
            return Result.Fail<Guid>(
                new ErroNaoAutenticado("A sessão não possui um cidadão válido."));
        }

        return Result.Ok(usuarioId);
    }
}
