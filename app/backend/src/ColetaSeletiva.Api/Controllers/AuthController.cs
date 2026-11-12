using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColetaSeletiva.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RegistrarOrganizacaoHandler _registrarOrganizacao;

    public AuthController(RegistrarOrganizacaoHandler registrarOrganizacao)
    {
        _registrarOrganizacao = registrarOrganizacao;
    }

    [HttpPost("registro-organizacao")]
    [AllowAnonymous]
    [ProducesResponseType<RegistroDeOrganizacaoResposta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegistrarOrganizacao(
        RegistroDeOrganizacaoEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _registrarOrganizacao.ExecutarAsync(
            entrada,
            ObterCorrelacaoId(),
            cancellationToken);

        if (resultado.IsSuccess)
        {
            return Created(
                $"/api/v1/organizacoes/{resultado.Value.OrganizacaoId}",
                resultado.Value);
        }

        return this.ParaProblemDetails(resultado.Errors);
    }

    private Guid ObterCorrelacaoId()
    {
        if (Request.Headers.TryGetValue(
                ResultadoDeErroExtensions.CabecalhoDeCorrelacao,
                out var valores)
            && Guid.TryParse(valores.FirstOrDefault(), out var correlacaoId))
        {
            return correlacaoId;
        }

        return Guid.NewGuid();
    }
}
