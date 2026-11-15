using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;

namespace ColetaSeletiva.Application.Acesso.Equipe;

internal readonly record struct EscopoDeEquipe(
    Guid OrganizacaoId,
    Guid AtorUsuarioId);

internal static class EscopoDaEquipe
{
    public static Result<EscopoDeEquipe> Obter(ITenantContext tenantContext)
    {
        ArgumentNullException.ThrowIfNull(tenantContext);

        if (!tenantContext.Autenticado)
        {
            return Result.Fail<EscopoDeEquipe>(
                new ErroNaoAutenticado(
                    "É necessário autenticar para gerenciar a equipe."));
        }

        if (tenantContext.EhCidadao
            || !tenantContext.EscopoValido
            || tenantContext.OrganizacaoId is not Guid organizacaoId
            || tenantContext.UsuarioId is not Guid atorUsuarioId)
        {
            return Result.Fail<EscopoDeEquipe>(
                new ErroDeAcessoNegado(
                    "A gestão de equipe é exclusiva do administrador da organização."));
        }

        return Result.Ok(new EscopoDeEquipe(organizacaoId, atorUsuarioId));
    }
}
