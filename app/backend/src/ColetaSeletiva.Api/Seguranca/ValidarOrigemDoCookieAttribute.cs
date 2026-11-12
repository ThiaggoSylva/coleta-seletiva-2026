using ColetaSeletiva.Application.Erros;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace ColetaSeletiva.Api.Seguranca;

/// <summary>
/// Protege rotas que aceitam o cookie de refresh contra requisições
/// disparadas por outra origem. Exige a mesma origem da API e o cabeçalho
/// customizado enviado pela SPA.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class ValidarOrigemDoCookieAttribute : Attribute, IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requisicao = context.HttpContext.Request;
        var origem = requisicao.Headers[ConstantesDeSessao.CabecalhoDeOrigem].ToString();
        var configuracaoDeOrigem = context.HttpContext.RequestServices
            .GetService<ConfiguracaoDeOrigem>();
        var origemEsperada = configuracaoDeOrigem?.OrigemPermitida
            ?? $"{requisicao.Scheme}://{requisicao.Host}";
        var solicitadoCom = requisicao.Headers[ConstantesDeSessao.CabecalhoSolicitadoCom].ToString();

        if (string.Equals(origem, origemEsperada, StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                solicitadoCom,
                ConstantesDeSessao.ValorSolicitadoCom,
                StringComparison.Ordinal))
        {
            return;
        }

        var problema = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Acesso negado.",
            Detail = "A requisição não foi originada pela interface autorizada.",
        };

        problema.Extensions["code"] = CodigosDeErro.AcessoNegado;
        problema.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        context.Result = new ObjectResult(problema)
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
