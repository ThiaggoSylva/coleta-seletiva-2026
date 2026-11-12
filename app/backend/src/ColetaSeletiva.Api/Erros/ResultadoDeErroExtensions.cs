using ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;
using ColetaSeletiva.Application.Erros;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace ColetaSeletiva.Api.Erros;

internal static class ResultadoDeErroExtensions
{
    public const string CabecalhoDeCorrelacao = "X-Correlation-Id";

    public static IActionResult ParaProblemDetails(
        this ControllerBase controlador,
        IReadOnlyList<IError> erros)
    {
        var traceId = controlador.HttpContext.TraceIdentifier;
        var validacoes = erros.OfType<ErroDeValidacao>().ToList();

        if (validacoes.Count > 0)
        {
            var errosPorCampo = validacoes
                .GroupBy(erro => ParaCamelCase(erro.Campo), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => grupo.Select(erro => erro.Message).ToArray(),
                    StringComparer.OrdinalIgnoreCase);

            var validacao = new ValidationProblemDetails(errosPorCampo)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dados inválidos.",
            };

            validacao.Extensions["code"] = CodigosDeErro.Validacao;
            validacao.Extensions["traceId"] = traceId;

            return new BadRequestObjectResult(validacao);
        }

        var conflito = erros.OfType<ErroDeConflito>().FirstOrDefault();
        if (conflito is not null)
        {
            var problema = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = conflito.Message,
            };

            problema.Extensions["code"] = conflito.Codigo;
            problema.Extensions["traceId"] = traceId;

            return new ConflictObjectResult(problema);
        }

        var naoAutenticado = erros.OfType<ErroNaoAutenticado>().FirstOrDefault();
        if (naoAutenticado is not null)
        {
            return CriarProblema(
                StatusCodes.Status401Unauthorized,
                CodigosDeErro.NaoAutenticado,
                naoAutenticado.Message,
                traceId);
        }

        var acessoNegado = erros.OfType<ErroDeAcessoNegado>().FirstOrDefault();
        if (acessoNegado is not null)
        {
            return CriarProblema(
                StatusCodes.Status403Forbidden,
                CodigosDeErro.AcessoNegado,
                acessoNegado.Message,
                traceId);
        }

        var naoEncontrado = erros.OfType<ErroNaoEncontrado>().FirstOrDefault();
        if (naoEncontrado is not null)
        {
            return CriarProblema(
                StatusCodes.Status404NotFound,
                CodigosDeErro.NaoEncontrado,
                naoEncontrado.Message,
                traceId);
        }

        var interno = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Não foi possível concluir a operação.",
        };

        interno.Extensions["code"] = CodigosDeErro.FalhaDeCadastro;
        interno.Extensions["traceId"] = traceId;

        return new ObjectResult(interno)
        {
            StatusCode = StatusCodes.Status500InternalServerError,
        };
    }

    private static ObjectResult CriarProblema(
        int status,
        string codigo,
        string titulo,
        string traceId)
    {
        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
        };

        problema.Extensions["code"] = codigo;
        problema.Extensions["traceId"] = traceId;

        return new ObjectResult(problema)
        {
            StatusCode = status,
        };
    }

    private static string ParaCamelCase(string valor)
    {
        return string.IsNullOrEmpty(valor)
            ? valor
            : char.ToLowerInvariant(valor[0]) + valor[1..];
    }
}
