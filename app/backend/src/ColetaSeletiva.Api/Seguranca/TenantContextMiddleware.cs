using System.Security.Claims;
using ColetaSeletiva.Domain.Identidade;
using ColetaSeletiva.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;

namespace ColetaSeletiva.Api.Seguranca;

public sealed class TenantContextMiddleware
{
    private const string ClaimSubject = "sub";

    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, TenantContext tenantContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(tenantContext);

        tenantContext.Limpar();

        if (httpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await _next(httpContext);
            return;
        }

        var usuario = httpContext.User;
        if (usuario.Identity?.IsAuthenticated == true)
        {
            var usuarioId = ObterUsuarioId(usuario);
            var organizacaoId = ObterGuid(usuario, ClaimsColetaSeletiva.OrganizacaoId);
            var ehCidadao = usuario.IsInRole(Papeis.Cidadao);
            var ehOrganizacao = usuario.IsInRole(Papeis.Administrador)
                || usuario.IsInRole(Papeis.Operador);

            if (usuarioId is not null
                && ehCidadao
                && !ehOrganizacao
                && organizacaoId is null)
            {
                tenantContext.DefinirEscopo(usuarioId.Value, null, ehCidadao: true);
            }
            else if (usuarioId is not null
                     && ehOrganizacao
                     && !ehCidadao
                     && organizacaoId is not null)
            {
                tenantContext.DefinirEscopo(usuarioId.Value, organizacaoId, ehCidadao: false);
            }
            else
            {
                tenantContext.DefinirAutenticacaoInvalida(usuarioId);
            }
        }

        await _next(httpContext);
    }

    private static Guid? ObterUsuarioId(ClaimsPrincipal usuario)
    {
        return ObterGuid(usuario, ClaimSubject)
            ?? ObterGuid(usuario, ClaimTypes.NameIdentifier);
    }

    private static Guid? ObterGuid(ClaimsPrincipal usuario, string tipo)
    {
        var valor = usuario.FindFirst(tipo)?.Value;
        return Guid.TryParse(valor, out var guid) && guid != Guid.Empty ? guid : null;
    }
}
