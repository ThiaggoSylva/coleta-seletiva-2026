using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Application.Retiradas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColetaSeletiva.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/retiradas")]
public sealed class RetiradasController : ControllerBase
{
    private readonly ListarOrganizacoesElegiveisHandler _listarOrganizacoes;
    private readonly ListarJanelasDeRetiradaHandler _listarJanelas;
    private readonly CriarRetiradaHandler _criar;
    private readonly ListarMinhasRetiradasHandler _listarMinhas;
    private readonly ObterMinhaRetiradaHandler _obterMinha;
    private readonly CancelarRetiradaHandler _cancelar;
    private readonly ListarRetiradasHandler _listar;
    private readonly ObterRetiradaHandler _obter;
    private readonly AgendarRetiradaHandler _agendar;
    private readonly ReagendarRetiradaHandler _reagendar;
    private readonly RecusarRetiradaHandler _recusar;
    private readonly ExecutarRetiradaHandler _executar;
    private readonly MarcarRetiradaNaoRealizadaHandler _naoRealizada;

    public RetiradasController(
        ListarOrganizacoesElegiveisHandler listarOrganizacoes,
        ListarJanelasDeRetiradaHandler listarJanelas,
        CriarRetiradaHandler criar,
        ListarMinhasRetiradasHandler listarMinhas,
        ObterMinhaRetiradaHandler obterMinha,
        CancelarRetiradaHandler cancelar,
        ListarRetiradasHandler listar,
        ObterRetiradaHandler obter,
        AgendarRetiradaHandler agendar,
        ReagendarRetiradaHandler reagendar,
        RecusarRetiradaHandler recusar,
        ExecutarRetiradaHandler executar,
        MarcarRetiradaNaoRealizadaHandler naoRealizada)
    {
        _listarOrganizacoes = listarOrganizacoes;
        _listarJanelas = listarJanelas;
        _criar = criar;
        _listarMinhas = listarMinhas;
        _obterMinha = obterMinha;
        _cancelar = cancelar;
        _listar = listar;
        _obter = obter;
        _agendar = agendar;
        _reagendar = reagendar;
        _recusar = recusar;
        _executar = executar;
        _naoRealizada = naoRealizada;
    }

    [HttpGet("organizacoes")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteCidadao)]
    [ProducesResponseType<IReadOnlyList<OrganizacaoParaRetiradaResposta>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarOrganizacoes(
        [FromQuery] int municipioId,
        CancellationToken cancellationToken)
    {
        var organizacoes = await _listarOrganizacoes.ExecutarAsync(
            municipioId,
            cancellationToken);

        return Ok(organizacoes);
    }

    [HttpGet("janelas")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteCidadao)]
    [ProducesResponseType<IReadOnlyList<JanelaDeRetiradaResposta>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarJanelas(
        [FromQuery] Guid organizacaoId,
        CancellationToken cancellationToken)
    {
        var resultado = await _listarJanelas.ExecutarAsync(organizacaoId, cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPost]
    [Authorize(Policy = PoliticasDeAcesso.SomenteCidadao)]
    [ProducesResponseType<RetiradaDetalhe>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar(
        CriarRetiradaEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _criar.ExecutarAsync(
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Created($"/api/v1/retiradas/minhas/{resultado.Value.Id}", resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpGet("minhas")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteCidadao)]
    [ProducesResponseType<PaginaDeRetiradas>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarMinhas(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        var resultado = await _listarMinhas.ExecutarAsync(page, pageSize, cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpGet("minhas/{id:guid}")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteCidadao)]
    [ProducesResponseType<RetiradaDetalhe>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterMinha(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _obterMinha.ExecutarAsync(id, cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPost("minhas/{id:guid}/cancelar")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteCidadao)]
    [ProducesResponseType<RetiradaDetalhe>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _cancelar.ExecutarAsync(
            id,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpGet]
    [Authorize(Policy = PoliticasDeAcesso.SomenteEquipeDaOrganizacao)]
    [ProducesResponseType<PaginaDeRetiradas>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? estado,
        [FromQuery] DateOnly? data,
        [FromQuery] int? municipioId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        var resultado = await _listar.ExecutarAsync(
            new FiltroDeRetiradasDaOrganizacao(estado, data, municipioId, page, pageSize),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteEquipeDaOrganizacao)]
    [ProducesResponseType<RetiradaDetalhe>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _obter.ExecutarAsync(id, cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPost("{id:guid}/agendar")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteEquipeDaOrganizacao)]
    [ProducesResponseType<RetiradaDetalhe>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Agendar(
        Guid id,
        AgendarRetiradaEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _agendar.ExecutarAsync(
            id,
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPost("{id:guid}/reagendar")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteEquipeDaOrganizacao)]
    [ProducesResponseType<RetiradaDetalhe>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reagendar(
        Guid id,
        ReagendarRetiradaEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _reagendar.ExecutarAsync(
            id,
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPost("{id:guid}/recusar")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteEquipeDaOrganizacao)]
    [ProducesResponseType<RetiradaDetalhe>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Recusar(
        Guid id,
        RecusarRetiradaEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _recusar.ExecutarAsync(
            id,
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPost("{id:guid}/executar")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteEquipeDaOrganizacao)]
    [ProducesResponseType<RetiradaDetalhe>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Executar(
        Guid id,
        ExecutarRetiradaEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _executar.ExecutarAsync(
            id,
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPost("{id:guid}/nao-realizada")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteEquipeDaOrganizacao)]
    [ProducesResponseType<RetiradaDetalhe>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> NaoRealizada(
        Guid id,
        NaoRealizadaEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _naoRealizada.ExecutarAsync(
            id,
            entrada,
            Request.ObterCorrelacaoId(),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }
}
