using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Application.PerfilDaOrganizacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColetaSeletiva.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasDeAcesso.SomenteAdministrador)]
[Route("api/v1/organizacao")]
public sealed class OrganizacaoController : ControllerBase
{
    private readonly ObterPerfilDaOrganizacaoHandler _obterPerfil;
    private readonly AtualizarPerfilDaOrganizacaoHandler _atualizarPerfil;
    private readonly DefinirMunicipiosHandler _definirMunicipios;
    private readonly DefinirPeriodosHandler _definirPeriodos;
    private readonly DefinirTiposHabilitadosHandler _definirTipos;
    private readonly DefinirPublicacaoHandler _definirPublicacao;

    public OrganizacaoController(
        ObterPerfilDaOrganizacaoHandler obterPerfil,
        AtualizarPerfilDaOrganizacaoHandler atualizarPerfil,
        DefinirMunicipiosHandler definirMunicipios,
        DefinirPeriodosHandler definirPeriodos,
        DefinirTiposHabilitadosHandler definirTipos,
        DefinirPublicacaoHandler definirPublicacao)
    {
        _obterPerfil = obterPerfil;
        _atualizarPerfil = atualizarPerfil;
        _definirMunicipios = definirMunicipios;
        _definirPeriodos = definirPeriodos;
        _definirTipos = definirTipos;
        _definirPublicacao = definirPublicacao;
    }

    [HttpGet]
    [ProducesResponseType<PerfilDaOrganizacaoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(CancellationToken cancellationToken)
    {
        var resultado = await _obterPerfil.ExecutarAsync(cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPut]
    [ProducesResponseType<PerfilDaOrganizacaoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(
        AtualizarPerfilDaOrganizacaoEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _atualizarPerfil.ExecutarAsync(
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPut("publicacao")]
    [ProducesResponseType<PublicacaoDaOrganizacaoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DefinirPublicacao(
        PublicacaoDaOrganizacaoEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _definirPublicacao.ExecutarAsync(
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPut("municipios")]
    [ProducesResponseType<PerfilDaOrganizacaoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DefinirMunicipios(
        DefinirMunicipiosEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _definirMunicipios.ExecutarAsync(
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPut("periodos")]
    [ProducesResponseType<PerfilDaOrganizacaoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DefinirPeriodos(
        DefinirPeriodosEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _definirPeriodos.ExecutarAsync(
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpGet("tipos")]
    [ProducesResponseType<IReadOnlyList<TipoHabilitadoResposta>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarTipos(CancellationToken cancellationToken)
    {
        var resultado = await _obterPerfil.ExecutarAsync(cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value.Tipos)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPut("tipos")]
    [ProducesResponseType<PerfilDaOrganizacaoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DefinirTipos(
        DefinirTiposHabilitadosEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _definirTipos.ExecutarAsync(
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }
}
