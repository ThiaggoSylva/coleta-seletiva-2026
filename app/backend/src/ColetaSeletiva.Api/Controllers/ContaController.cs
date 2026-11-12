using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Application.Acesso.PerfilDoCidadao;
using Microsoft.AspNetCore.Mvc;

namespace ColetaSeletiva.Api.Controllers;

[ApiController]
[Route("api/v1/conta")]
public sealed class ContaController : ControllerBase
{
    private readonly ObterPerfilDoCidadaoHandler _obterPerfil;
    private readonly AtualizarPerfilDoCidadaoHandler _atualizarPerfil;

    public ContaController(
        ObterPerfilDoCidadaoHandler obterPerfil,
        AtualizarPerfilDoCidadaoHandler atualizarPerfil)
    {
        _obterPerfil = obterPerfil;
        _atualizarPerfil = atualizarPerfil;
    }

    [HttpGet]
    [ProducesResponseType<PerfilDoCidadao>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Obter(CancellationToken cancellationToken)
    {
        var resultado = await _obterPerfil.ExecutarAsync(cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPut]
    [ProducesResponseType<PerfilDoCidadao>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Atualizar(
        AtualizarPerfilDoCidadaoEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _atualizarPerfil.ExecutarAsync(entrada, cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }
}
