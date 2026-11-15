using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Application.Catalogo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColetaSeletiva.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/catalogo")]
public sealed class CatalogoController : ControllerBase
{
    private readonly ConsultarCatalogoDeTiposHandler _consultarTipos;
    private readonly ConsultarMunicipiosHandler _consultarMunicipios;

    public CatalogoController(
        ConsultarCatalogoDeTiposHandler consultarTipos,
        ConsultarMunicipiosHandler consultarMunicipios)
    {
        _consultarTipos = consultarTipos;
        _consultarMunicipios = consultarMunicipios;
    }

    [HttpGet("tipos")]
    [ProducesResponseType<IReadOnlyList<TipoResiduoDoCatalogo>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListarTipos(CancellationToken cancellationToken)
    {
        var tipos = await _consultarTipos.ExecutarAsync(cancellationToken);
        return Ok(tipos);
    }

    [HttpGet("municipios")]
    [ProducesResponseType<IReadOnlyList<MunicipioDoCatalogo>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListarMunicipios(
        [FromQuery] string? uf,
        [FromQuery] string? texto,
        CancellationToken cancellationToken)
    {
        var resultado = await _consultarMunicipios.ExecutarAsync(uf, texto, cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }
}
