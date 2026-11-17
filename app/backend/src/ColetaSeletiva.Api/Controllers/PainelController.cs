using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Application.Retiradas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColetaSeletiva.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasDeAcesso.SomenteAdministrador)]
[Route("api/v1/painel")]
public sealed class PainelController : ControllerBase
{
    private readonly ObterResumoDoPainelHandler _resumo;
    private readonly ObterSerieDoPainelHandler _serie;

    public PainelController(
        ObterResumoDoPainelHandler resumo,
        ObterSerieDoPainelHandler serie)
    {
        _resumo = resumo;
        _serie = serie;
    }

    [HttpGet("resumo")]
    [ProducesResponseType<ResumoDoPainel>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterResumo(
        [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate,
        [FromQuery] Guid? tipoResiduoId,
        [FromQuery] string? origem,
        CancellationToken cancellationToken)
    {
        var resultado = await _resumo.ExecutarAsync(
            new FiltroDoPainel(de, ate, tipoResiduoId, origem),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpGet("serie")]
    [ProducesResponseType<SerieDoPainel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ObterSerie(
        [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate,
        [FromQuery] Guid? tipoResiduoId,
        [FromQuery] string? origem,
        [FromQuery] string agrupamento = "dia",
        CancellationToken cancellationToken = default)
    {
        var resultado = await _serie.ExecutarAsync(
            new FiltroDoPainel(de, ate, tipoResiduoId, origem),
            agrupamento,
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }
}

[ApiController]
[Authorize(Policy = PoliticasDeAcesso.SomenteAdministrador)]
[Route("api/v1/relatorios")]
public sealed class RelatoriosController : ControllerBase
{
    private readonly ExportarVolumesCsvHandler _exportar;

    public RelatoriosController(ExportarVolumesCsvHandler exportar)
    {
        _exportar = exportar;
    }

    [HttpGet("volumes.csv")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportarVolumes(
        [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate,
        [FromQuery] Guid? tipoResiduoId,
        [FromQuery] string? origem,
        CancellationToken cancellationToken)
    {
        var resultado = await _exportar.ExecutarAsync(
            new FiltroDoPainel(de, ate, tipoResiduoId, origem),
            cancellationToken);

        return resultado.IsSuccess
            ? File(
                resultado.Value.Conteudo,
                "text/csv; charset=utf-8",
                resultado.Value.NomeArquivo)
            : this.ParaProblemDetails(resultado.Errors);
    }
}
