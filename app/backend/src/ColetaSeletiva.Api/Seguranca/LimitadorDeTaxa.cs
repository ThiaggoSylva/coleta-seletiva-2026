using System.Globalization;
using System.Threading.RateLimiting;
using ColetaSeletiva.Api.Configuration;
using ColetaSeletiva.Application.Erros;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ColetaSeletiva.Api.Seguranca;

/// <summary>
/// Limita tentativas por rota e origem com janela fixa. A resposta de recusa
/// é um ProblemDetails genérico, sem indicar se a conta existe e sem PII.
/// </summary>
public static class LimitadorDeTaxa
{
    public static IServiceCollection AdicionarLimitadorDeTaxa(
        this IServiceCollection services,
        ConfiguracaoDeLimitesDeTaxa limites)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(limites);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = ResponderRecusa;

            AdicionarPoliticaDeJanelaFixa(options, PoliticasDeLimiteDeTaxa.Login, limites.Login);
            AdicionarPoliticaDeJanelaFixa(options, PoliticasDeLimiteDeTaxa.Refresh, limites.Refresh);
            AdicionarPoliticaDeJanelaFixa(options, PoliticasDeLimiteDeTaxa.Cadastro, limites.Cadastro);
        });

        return services;
    }

    private static void AdicionarPoliticaDeJanelaFixa(
        RateLimiterOptions options,
        string nome,
        ConfiguracaoDeLimiteDeTaxa limite)
    {
        options.AddPolicy(nome, contexto =>
            RateLimitPartition.GetFixedWindowLimiter(
                $"{nome}:{ObterOrigem(contexto)}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limite.PermitLimit,
                    Window = TimeSpan.FromSeconds(limite.WindowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
    }

    private static string ObterOrigem(HttpContext contexto)
    {
        return contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecida";
    }

    private static async ValueTask ResponderRecusa(
        OnRejectedContext contexto,
        CancellationToken cancellationToken)
    {
        var resposta = contexto.HttpContext.Response;
        resposta.StatusCode = StatusCodes.Status429TooManyRequests;
        resposta.Headers.CacheControl = "no-store";

        if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            resposta.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds)
                .ToString(CultureInfo.InvariantCulture);
        }

        var problema = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Muitas tentativas. Tente novamente mais tarde.",
        };

        problema.Extensions["code"] = CodigosDeErro.LimiteDeTaxa;
        problema.Extensions["traceId"] = contexto.HttpContext.TraceIdentifier;

        await resposta.WriteAsJsonAsync(
            problema,
            options: null,
            contentType: "application/problem+json",
            cancellationToken);
    }
}
