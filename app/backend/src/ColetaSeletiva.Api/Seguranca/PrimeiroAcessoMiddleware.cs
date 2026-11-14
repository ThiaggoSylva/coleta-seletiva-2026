using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Domain.Identidade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColetaSeletiva.Api.Seguranca;

/// <summary>
/// Enquanto a troca de senha inicial estiver pendente, permite apenas as
/// rotas de consulta da conta, troca de senha e logout. Rotas públicas de
/// autenticação continuam acessíveis para permitir login e renovação.
/// </summary>
public sealed class PrimeiroAcessoMiddleware
{
    private const string MarcaTrocaPendente = "true";

    private static readonly string[] CaminhosPermitidos =
    [
        "/api/v1/auth/me",
        "/api/v1/auth/alterar-senha",
        "/api/v1/auth/logout",
    ];

    private readonly RequestDelegate _next;

    public PrimeiroAcessoMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (httpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await _next(httpContext);
            return;
        }

        var usuario = httpContext.User;
        var caminho = httpContext.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
        var bloqueado = usuario.Identity?.IsAuthenticated == true
            && usuario.HasClaim(
                ClaimsColetaSeletiva.TrocaSenhaPendente,
                MarcaTrocaPendente)
            && !CaminhosPermitidos.Contains(caminho, StringComparer.OrdinalIgnoreCase);

        if (!bloqueado)
        {
            await _next(httpContext);
            return;
        }

        var problema = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Acesso negado.",
            Detail = "Conclua a troca de senha para acessar este recurso.",
        };

        problema.Extensions["code"] = CodigosDeErro.AcessoNegado;
        problema.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
        await httpContext.Response.WriteAsJsonAsync(
            problema,
            options: null,
            contentType: "application/problem+json",
            httpContext.RequestAborted);
    }
}
