using ColetaSeletiva.Api.Contratos;
using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Application.Acesso.RegistroDeCidadao;
using ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;
using ColetaSeletiva.Application.Acesso.Sessao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ColetaSeletiva.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RegistrarOrganizacaoHandler _registrarOrganizacao;
    private readonly RegistrarCidadaoHandler _registrarCidadao;
    private readonly IniciarSessaoHandler _iniciarSessao;
    private readonly RenovarSessaoHandler _renovarSessao;
    private readonly EncerrarSessaoHandler _encerrarSessao;
    private readonly AlterarSenhaHandler _alterarSenha;
    private readonly ObterContaAtualHandler _obterContaAtual;

    public AuthController(
        RegistrarOrganizacaoHandler registrarOrganizacao,
        RegistrarCidadaoHandler registrarCidadao,
        IniciarSessaoHandler iniciarSessao,
        RenovarSessaoHandler renovarSessao,
        EncerrarSessaoHandler encerrarSessao,
        AlterarSenhaHandler alterarSenha,
        ObterContaAtualHandler obterContaAtual)
    {
        _registrarOrganizacao = registrarOrganizacao;
        _registrarCidadao = registrarCidadao;
        _iniciarSessao = iniciarSessao;
        _renovarSessao = renovarSessao;
        _encerrarSessao = encerrarSessao;
        _alterarSenha = alterarSenha;
        _obterContaAtual = obterContaAtual;
    }

    [HttpPost("registro-organizacao")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasDeLimiteDeTaxa.Cadastro)]
    [ProducesResponseType<RegistroDeOrganizacaoResposta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegistrarOrganizacao(
        RegistroDeOrganizacaoEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _registrarOrganizacao.ExecutarAsync(
            entrada,
            ObterCorrelacaoId(),
            cancellationToken);

        if (resultado.IsSuccess)
        {
            return Created(
                $"/api/v1/organizacoes/{resultado.Value.OrganizacaoId}",
                resultado.Value);
        }

        return this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPost("registro-cidadao")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasDeLimiteDeTaxa.Cadastro)]
    [ProducesResponseType<RegistroDeCidadaoResposta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegistrarCidadao(
        RegistroDeCidadaoEntrada entrada,
        CancellationToken cancellationToken)
    {
        var resultado = await _registrarCidadao.ExecutarAsync(
            entrada,
            ObterCorrelacaoId(),
            cancellationToken);

        if (resultado.IsSuccess)
        {
            return Created("/api/v1/conta", resultado.Value);
        }

        return this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasDeLimiteDeTaxa.Login)]
    [ProducesResponseType<SessaoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login(
        LoginEntrada entrada,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        var resultado = await _iniciarSessao.ExecutarAsync(
            entrada,
            ObterCorrelacaoId(),
            cancellationToken);

        if (resultado.IsFailed)
        {
            return this.ParaProblemDetails(resultado.Errors);
        }

        var sessao = resultado.Value;
        DefinirCookieDeRefresh(sessao.RefreshToken, sessao.RefreshExpiraEmUtc);

        return Ok(MapearSessao(sessao));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<ContaDaSessaoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObterContaAtual(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        var resultado = await _obterContaAtual.ExecutarAsync(cancellationToken);

        return resultado.IsSuccess
            ? Ok(MapearConta(resultado.Value))
            : this.ParaProblemDetails(resultado.Errors);
    }

    [HttpPost("alterar-senha")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AlterarSenha(
        AlterarSenhaEntrada entrada,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        var resultado = await _alterarSenha.ExecutarAsync(
            entrada,
            ObterCorrelacaoId(),
            cancellationToken);

        if (resultado.IsFailed)
        {
            return this.ParaProblemDetails(resultado.Errors);
        }

        LimparCookieDeRefresh();
        return NoContent();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasDeLimiteDeTaxa.Refresh)]
    [ValidarOrigemDoCookie]
    [ProducesResponseType<SessaoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Renovar(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        var resultado = await _renovarSessao.ExecutarAsync(
            ObterRefreshDoCookie(),
            cancellationToken);

        if (resultado.IsFailed)
        {
            LimparCookieDeRefresh();
            return this.ParaProblemDetails(resultado.Errors);
        }

        var sessao = resultado.Value;
        DefinirCookieDeRefresh(sessao.RefreshToken, sessao.RefreshExpiraEmUtc);

        return Ok(MapearSessao(sessao));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ValidarOrigemDoCookie]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Encerrar(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        var resultado = await _encerrarSessao.ExecutarAsync(
            ObterRefreshDoCookie(),
            ObterCorrelacaoId(),
            cancellationToken);

        LimparCookieDeRefresh();

        return resultado.IsSuccess
            ? NoContent()
            : this.ParaProblemDetails(resultado.Errors);
    }

    private static SessaoResposta MapearSessao(SessaoEmitida sessao)
    {
        return new SessaoResposta(
            TokenDeAcesso: sessao.TokenDeAcesso,
            ExpiraEmUtc: sessao.ExpiraEmUtc,
            Usuario: MapearConta(sessao.Conta));
    }

    private static ContaDaSessaoResposta MapearConta(ContaAutenticada conta)
    {
        return new ContaDaSessaoResposta(
            Id: conta.Id,
            Nome: conta.Nome,
            Email: conta.Email,
            Papel: conta.Papel,
            OrganizacaoId: conta.OrganizacaoId,
            TrocaSenhaPendente: conta.TrocaSenhaPendente);
    }

    private string? ObterRefreshDoCookie()
    {
        return Request.Cookies.TryGetValue(ConstantesDeSessao.NomeDoCookie, out var valor)
            && !string.IsNullOrWhiteSpace(valor)
                ? valor
                : null;
    }

    private void DefinirCookieDeRefresh(string valor, DateTimeOffset expiraEmUtc)
    {
        Response.Cookies.Append(
            ConstantesDeSessao.NomeDoCookie,
            valor,
            CriarOpcoesDoCookie(expiraEmUtc));
    }

    private void LimparCookieDeRefresh()
    {
        Response.Cookies.Delete(
            ConstantesDeSessao.NomeDoCookie,
            CriarOpcoesDoCookie(expiraEmUtc: null));
    }

    private static CookieOptions CriarOpcoesDoCookie(DateTimeOffset? expiraEmUtc)
    {
        var opcoes = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = ConstantesDeSessao.CaminhoDoCookie,
            IsEssential = true,
        };

        if (expiraEmUtc is DateTimeOffset expiracao)
        {
            opcoes.Expires = expiracao;
        }

        return opcoes;
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
