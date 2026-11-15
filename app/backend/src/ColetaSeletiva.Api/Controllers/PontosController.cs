using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Application.Pontos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColetaSeletiva.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/pontos")]
public sealed class PontosController : ControllerBase
{
    private readonly ListarPontosHandler _listar;
    private readonly ObterPontoHandler _obter;
    private readonly ListarOpcoesDePontosHandler _opcoes;
    private readonly CriarPontoHandler _criar;
    private readonly AtualizarPontoHandler _atualizar;
    private readonly AlterarStatusPontoHandler _alterarStatus;
    private readonly RetirarPontoHandler _retirar;

    public PontosController(
        ListarPontosHandler listar,
        ObterPontoHandler obter,
        ListarOpcoesDePontosHandler opcoes,
        CriarPontoHandler criar,
        AtualizarPontoHandler atualizar,
        AlterarStatusPontoHandler alterarStatus,
        RetirarPontoHandler retirar)
    {
        _listar = listar;
        _obter = obter;
        _opcoes = opcoes;
        _criar = criar;
        _atualizar = atualizar;
        _alterarStatus = alterarStatus;
        _retirar = retirar;
    }

    [HttpGet]
    [Authorize(Policy = PoliticasDeAcesso.SomenteAdministrador)]
    [ProducesResponseType<IReadOnlyList<PontoResumo>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var resultado = await _listar.ExecutarAsync(cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpGet("opcoes")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteEquipeDaOrganizacao)]
    [ProducesResponseType<IReadOnlyList<OpcaoDePonto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarOpcoes(CancellationToken cancellationToken)
    {
        var resultado = await _opcoes.ExecutarAsync(cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteAdministrador)]
    [ProducesResponseType<PontoDetalhe>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _obter.ExecutarAsync(id, cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPost]
    [Authorize(Policy = PoliticasDeAcesso.SomenteAdministrador)]
    [ProducesResponseType<PontoDetalhe>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar(
        CriarPontoEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _criar.ExecutarAsync(
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Created($"/api/v1/pontos/{resultado.Value.Id}", resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteAdministrador)]
    [ProducesResponseType<PontoDetalhe>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Atualizar(
        Guid id,
        AtualizarPontoEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _atualizar.ExecutarAsync(
            id,
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteAdministrador)]
    [ProducesResponseType<PontoDetalhe>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AlterarStatus(
        Guid id,
        AlterarStatusPontoEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _alterarStatus.ExecutarAsync(
            id,
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteAdministrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Retirar(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _retirar.ExecutarAsync(
            id,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? NoContent()
            : this.ParaProblemDetails(resultado.Errors);
    }
}
