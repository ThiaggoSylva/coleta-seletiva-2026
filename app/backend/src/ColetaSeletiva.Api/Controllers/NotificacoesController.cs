using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Application.Retiradas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColetaSeletiva.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notificacoes")]
public sealed class NotificacoesController : ControllerBase
{
    private readonly ListarNotificacoesHandler _listar;
    private readonly MarcarNotificacaoLidaHandler _marcarLida;
    private readonly MarcarTodasNotificacoesLidasHandler _marcarTodas;

    public NotificacoesController(
        ListarNotificacoesHandler listar,
        MarcarNotificacaoLidaHandler marcarLida,
        MarcarTodasNotificacoesLidasHandler marcarTodas)
    {
        _listar = listar;
        _marcarLida = marcarLida;
        _marcarTodas = marcarTodas;
    }

    [HttpGet]
    [ProducesResponseType<ListaDeNotificacoesResposta>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] DateTimeOffset? desde,
        [FromQuery] bool apenasNaoLidas,
        CancellationToken cancellationToken)
    {
        var resultado = await _listar.ExecutarAsync(
            desde,
            apenasNaoLidas,
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPost("{id:guid}/lida")]
    [ProducesResponseType<NotificacaoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarLida(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _marcarLida.ExecutarAsync(id, cancellationToken);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPost("lidas")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarcarTodas(CancellationToken cancellationToken)
    {
        var resultado = await _marcarTodas.ExecutarAsync(cancellationToken);

        return resultado.IsSuccess
            ? Ok(new { atualizadas = resultado.Value })
            : this.ParaProblemDetails(resultado.Errors);
    }
}
