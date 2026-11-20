using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Application.Roadmap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ColetaSeletiva.Api.Controllers;

/// <summary>
/// Superfície única de sinalização do roadmap (DT-36). O índice é público;
/// cada detalhe é protegido conforme o público do módulo e responde 501.
/// </summary>
[ApiController]
[Route("api/v1/em-breve")]
[EnableRateLimiting(PoliticasDeLimiteDeTaxa.ConsultaPublica)]
public sealed class EmBreveController : ControllerBase
{
    private readonly ConsultarPlaceholdersHandler _consultar;

    public EmBreveController(ConsultarPlaceholdersHandler consultar)
    {
        _consultar = consultar;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<ModuloEmBreve>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public IActionResult Indice()
    {
        Response.Headers.CacheControl = "public, max-age=300";

        return Ok(_consultar.Listar());
    }

    [HttpGet("preferencias-email")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteCidadao)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public IActionResult PreferenciasEmail() => Detalhe("preferencias-email");

    [HttpGet("extrato-pontos")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteCidadao)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public IActionResult ExtratoPontos() => Detalhe("extrato-pontos");

    [HttpGet("rotas-coletor")]
    [Authorize(Policy = PoliticasDeAcesso.SomenteEquipeDaOrganizacao)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public IActionResult RotasColetor() => Detalhe("rotas-coletor");

    [HttpGet("transparencia-impacto")]
    [AllowAnonymous]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public IActionResult TransparenciaImpacto() => Detalhe("transparencia-impacto");

    /// <summary>
    /// Rotas literais acima têm precedência; este fallback responde 404 para
    /// identificadores fora do catálogo, sem revelar o valor recebido.
    /// </summary>
    [HttpGet("{modulo}")]
    [AllowAnonymous]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public IActionResult ModuloDesconhecido([FromRoute] string modulo)
    {
        return this.ParaProblemDetailsDeModuloNaoEncontrado();
    }

    private IActionResult Detalhe(string id)
    {
        var modulo = _consultar.ObterPorId(id);

        return modulo is null
            ? this.ParaProblemDetailsDeModuloNaoEncontrado()
            : this.ParaProblemDetailsDeEmBreve(modulo);
    }
}
