using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Application.Catalogo;
using ColetaSeletiva.Application.ConsultaPublica;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;

namespace ColetaSeletiva.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/publico")]
[EnableRateLimiting(PoliticasDeLimiteDeTaxa.ConsultaPublica)]
public sealed class PublicoController : ControllerBase
{
    private readonly BuscarPontosPublicosHandler _buscarPontos;
    private readonly ConsultarMunicipiosHandler _consultarMunicipios;

    public PublicoController(
        BuscarPontosPublicosHandler buscarPontos,
        ConsultarMunicipiosHandler consultarMunicipios)
    {
        _buscarPontos = buscarPontos;
        _consultarMunicipios = consultarMunicipios;
    }

    [HttpPost("pontos/busca")]
    [ProducesResponseType<PaginaDePontosPublicos>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> BuscarPontos(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]
        BuscaPublicaDePontosEntrada? entrada,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "public, max-age=30";

        var resultado = await _buscarPontos.ExecutarAsync(entrada, cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpGet("municipios")]
    [ProducesResponseType<IReadOnlyList<MunicipioDoCatalogo>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ListarMunicipios(
        [FromQuery] string? uf,
        [FromQuery] string? texto,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "public, max-age=300";

        var resultado = await _consultarMunicipios.ExecutarAsync(uf, texto, cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }
}
