using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Application.Retiradas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColetaSeletiva.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasDeAcesso.SomenteEquipeDaOrganizacao)]
[Route("api/v1/volumes")]
public sealed class VolumesController : ControllerBase
{
    private readonly ListarVolumesHandler _listar;
    private readonly RegistrarRecebimentoHandler _registrarRecebimento;
    private readonly AjustarVolumeHandler _ajustar;

    public VolumesController(
        ListarVolumesHandler listar,
        RegistrarRecebimentoHandler registrarRecebimento,
        AjustarVolumeHandler ajustar)
    {
        _listar = listar;
        _registrarRecebimento = registrarRecebimento;
        _ajustar = ajustar;
    }

    [HttpGet]
    [ProducesResponseType<PaginaDeVolumes>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate,
        [FromQuery] Guid? tipoResiduoId,
        [FromQuery] string? origem,
        [FromQuery] Guid? pontoId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        var resultado = await _listar.ExecutarAsync(
            new FiltroDeVolumes(de, ate, tipoResiduoId, origem, pontoId, page, pageSize),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPost("recebimentos")]
    [ProducesResponseType<VolumeResposta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegistrarRecebimento(
        RegistrarRecebimentoEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _registrarRecebimento.ExecutarAsync(
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Created($"/api/v1/volumes/{resultado.Value.Id}", resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPost("{id:guid}/ajustes")]
    [ProducesResponseType<VolumeResposta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Ajustar(
        Guid id,
        AjustarVolumeEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _ajustar.ExecutarAsync(
            id,
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Created($"/api/v1/volumes/{resultado.Value.Id}", resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }
}
