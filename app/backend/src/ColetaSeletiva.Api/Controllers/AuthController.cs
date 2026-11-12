using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Application.Acesso.RegistroDeCidadao;
using ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColetaSeletiva.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RegistrarOrganizacaoHandler _registrarOrganizacao;
    private readonly RegistrarCidadaoHandler _registrarCidadao;

    public AuthController(
        RegistrarOrganizacaoHandler registrarOrganizacao,
        RegistrarCidadaoHandler registrarCidadao)
    {
        _registrarOrganizacao = registrarOrganizacao;
        _registrarCidadao = registrarCidadao;
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

    [HttpPost("registro-cidadao")]
    [AllowAnonymous]
    [ProducesResponseType<RegistroDeCidadaoResposta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegistrarCidadao(
        RegistroDeCidadaoEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _registrarCidadao.ExecutarAsync(
            entrada,
            ObterCorrelacaoId(),
            cancellationToken);

        if (resultado.IsSuccess)
        {
            return Created("/api/v1/conta", resultado.Value);
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
