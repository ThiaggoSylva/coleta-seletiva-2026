using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;

namespace ColetaSeletiva.Application.Retiradas;

public readonly record struct EscopoDoCidadao(
    Guid CidadaoId,
    Guid AtorUsuarioId);

internal static class EscopoDoCidadaoHelper
{
    public static Result<EscopoDoCidadao> Obter(ITenantContext tenantContext)
    {
        ArgumentNullException.ThrowIfNull(tenantContext);

        if (!tenantContext.Autenticado)
        {
            return Result.Fail<EscopoDoCidadao>(
                new ErroNaoAutenticado("É necessário autenticar como cidadão."));
        }

        if (!tenantContext.EhCidadao
            || !tenantContext.EscopoValido
            || tenantContext.UsuarioId is not Guid cidadaoId)
        {
            return Result.Fail<EscopoDoCidadao>(
                new ErroDeAcessoNegado("A operação é exclusiva do cidadão."));
        }

        return Result.Ok(new EscopoDoCidadao(cidadaoId, cidadaoId));
    }
}
