using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Application.Acesso.Equipe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColetaSeletiva.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasDeAcesso.SomenteAdministrador)]
[Route("api/v1/equipe")]
public sealed class EquipeController : ControllerBase
{
    private const string RotaBase = "/api/v1/equipe";

    private readonly CriarOperadorHandler _criarOperador;
    private readonly ObterOperadorHandler _obterOperador;
    private readonly ListarOperadoresHandler _listarOperadores;
    private readonly EditarOperadorHandler _editarOperador;
    private readonly InativarOperadorHandler _inativarOperador;

    public EquipeController(
        CriarOperadorHandler criarOperador,
        ObterOperadorHandler obterOperador,
        ListarOperadoresHandler listarOperadores,
        EditarOperadorHandler editarOperador,
        InativarOperadorHandler inativarOperador)
    {
        _criarOperador = criarOperador;
        _obterOperador = obterOperador;
        _listarOperadores = listarOperadores;
        _editarOperador = editarOperador;
        _inativarOperador = inativarOperador;
    }

    [HttpPost]
    [ProducesResponseType<ContaDaEquipe>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Criar(
        CriarOperadorEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _criarOperador.ExecutarAsync(
            entrada,
            ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Created($"{RotaBase}/{resultado.Value.Id}", resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ContaDaEquipe>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Obter(
        Guid id,
        CancellationToken cancellationToken)
    {
        var resultado = await _obterOperador.ExecutarAsync(
            id,
            ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ContaDaEquipe>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var resultado = await _listarOperadores.ExecutarAsync(cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ContaDaEquipe>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Editar(
        Guid id,
        EditarOperadorEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _editarOperador.ExecutarAsync(
            id,
            entrada,
            ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Inativar(
        Guid id,
        CancellationToken cancellationToken)
    {
        var resultado = await _inativarOperador.ExecutarAsync(
            id,
            ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? NoContent()
            : this.ParaProblemDetails(resultado.Errors);
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
