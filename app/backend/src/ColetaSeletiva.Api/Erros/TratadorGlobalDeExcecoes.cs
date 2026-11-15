using ColetaSeletiva.Application.Erros;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ColetaSeletiva.Api.Erros;

/// <summary>
/// Converte exceções não tratadas em ProblemDetails genérico. O log registra
/// apenas o tipo da exceção e a correlação, nunca a mensagem ou dados da
/// requisição, que poderiam conter PII, SQL ou credenciais.
/// </summary>
public sealed class TratadorGlobalDeExcecoes : IExceptionHandler
{
    private readonly ILogger<TratadorGlobalDeExcecoes> _logger;

    public TratadorGlobalDeExcecoes(ILogger<TratadorGlobalDeExcecoes> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        _logger.LogError(
            "Erro não tratado ({TipoDaExcecao}) em {Metodo} {Caminho}. Correlação: {CorrelacaoId}.",
            exception.GetType().Name,
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            httpContext.TraceIdentifier);

        var problema = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Não foi possível concluir a operação.",
        };

        problema.Extensions["code"] = CodigosDeErro.ErroInterno;
        problema.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            problema,
            options: null,
            contentType: "application/problem+json",
            cancellationToken);

        return true;
    }
}
