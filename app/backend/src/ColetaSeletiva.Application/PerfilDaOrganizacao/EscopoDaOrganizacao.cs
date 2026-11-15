using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;

namespace ColetaSeletiva.Application.PerfilDaOrganizacao;

public readonly record struct EscopoDaOrganizacao(
    Guid OrganizacaoId,
    Guid AtorUsuarioId);

internal static class EscopoDaOrganizacaoHelper
{
    public static Result<EscopoDaOrganizacao> Obter(ITenantContext tenantContext)
    {
        ArgumentNullException.ThrowIfNull(tenantContext);

        if (!tenantContext.Autenticado)
        {
            return Result.Fail<EscopoDaOrganizacao>(
                new ErroNaoAutenticado(
                    "É necessário autenticar para gerenciar a organização."));
        }

        if (tenantContext.EhCidadao
            || !tenantContext.EscopoValido
            || tenantContext.OrganizacaoId is not Guid organizacaoId
            || tenantContext.UsuarioId is not Guid atorUsuarioId)
        {
            return Result.Fail<EscopoDaOrganizacao>(
                new ErroDeAcessoNegado(
                    "A gestão da organização é exclusiva do administrador."));
        }

        return Result.Ok(new EscopoDaOrganizacao(organizacaoId, atorUsuarioId));
    }
}
