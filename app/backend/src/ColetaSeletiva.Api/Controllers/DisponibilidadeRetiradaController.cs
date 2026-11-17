using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Application.Retiradas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColetaSeletiva.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasDeAcesso.SomenteAdministrador)]
[Route("api/v1/disponibilidade-retirada")]
public sealed class DisponibilidadeRetiradaController : ControllerBase
{
    private readonly ObterDisponibilidadeHandler _obter;
    private readonly DefinirDisponibilidadeHandler _definir;

    public DisponibilidadeRetiradaController(
        ObterDisponibilidadeHandler obter,
        DefinirDisponibilidadeHandler definir)
    {
        _obter = obter;
        _definir = definir;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DisponibilidadeDiaResposta>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Obter(CancellationToken cancellationToken)
    {
        var resultado = await _obter.ExecutarAsync(cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPut]
    [ProducesResponseType<IReadOnlyList<DisponibilidadeDiaResposta>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Definir(
        DisponibilidadeSemanaEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _definir.ExecutarAsync(
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }
}

[ApiController]
[Authorize(Policy = PoliticasDeAcesso.SomenteAdministrador)]
[Route("api/v1/bloqueios-retirada")]
public sealed class BloqueiosRetiradaController : ControllerBase
{
    private readonly ListarBloqueiosHandler _listar;
    private readonly CriarBloqueioHandler _criar;
    private readonly RemoverBloqueioHandler _remover;

    public BloqueiosRetiradaController(
        ListarBloqueiosHandler listar,
        CriarBloqueioHandler criar,
        RemoverBloqueioHandler remover)
    {
        _listar = listar;
        _criar = criar;
        _remover = remover;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BloqueioDeDataResposta>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var resultado = await _listar.ExecutarAsync(cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPost]
    [ProducesResponseType<BloqueioDeDataResposta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar(
        BloqueioDeDataEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _criar.ExecutarAsync(
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Created($"/api/v1/bloqueios-retirada/{resultado.Value.Id}", resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _remover.ExecutarAsync(
            id,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? NoContent()
            : this.ParaProblemDetails(resultado.Errors);
    }
}
