using System.Globalization;
using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.PerfilDaOrganizacao;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Seguranca;
using ColetaSeletiva.Domain.Entidades;
using FluentResults;
using FluentValidation;

namespace ColetaSeletiva.Application.Pontos;

/// <summary>
/// Base dos handlers de escrita de ponto: escopo, transação e auditoria (RF-08).
/// </summary>
public abstract class ManipuladorDePonto
{
    private readonly ITenantContext _tenantContext;
    private readonly IRegistroDeAuditoria _auditoria;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly TimeProvider _relogio;

    protected ManipuladorDePonto(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio)
    {
        _tenantContext = tenantContext;
        _auditoria = auditoria;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogio = relogio;
    }

    protected async Task<Result<PontoDetalhe>> ExecutarComAuditoriaAsync(
        Guid correlacaoId,
        string acao,
        string? camposAlterados,
        Func<Guid, Task<Result<PontoDetalhe>>> operacao,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operacao);

        if (correlacaoId == Guid.Empty)
        {
            throw new ArgumentException(
                "A correlação da operação não pode ser vazia.",
                nameof(correlacaoId));
        }

        var escopo = EscopoDaOrganizacaoHelper.Obter(_tenantContext);
        if (escopo.IsFailed)
        {
            return Result.Fail<PontoDetalhe>(escopo.Errors);
        }

        await using var transacao = await _unidadeDeTrabalho.IniciarAsync(cancellationToken);

        try
        {
            var resultado = await operacao(escopo.Value.OrganizacaoId);

            if (resultado.IsFailed)
            {
                await transacao.RollbackAsync(cancellationToken);
                return resultado;
            }

            await _auditoria.RegistrarAsync(
                new EventoDeAuditoria(
                    OrganizacaoId: escopo.Value.OrganizacaoId,
                    AtorUsuarioId: escopo.Value.AtorUsuarioId,
                    Acao: acao,
                    Entidade: EntidadesAuditadas.PontoColeta,
                    EntidadeId: resultado.Value.Id,
                    CorrelacaoId: correlacaoId,
                    OcorridoEmUtc: _relogio.GetUtcNow(),
                    CamposAlterados: camposAlterados),
                cancellationToken);

            await transacao.CommitAsync(cancellationToken);

            return resultado;
        }
        catch
        {
            await transacao.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public sealed class CriarPontoHandler : ManipuladorDePonto
{
    private const string CamposRegistrados =
        "nome,municipio,endereco,localizacao,status,horarios,tipos";

    private readonly IValidator<CriarPontoEntrada> _validador;
    private readonly IPontosDeColeta _pontos;

    public CriarPontoHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IValidator<CriarPontoEntrada> validador,
        IPontosDeColeta pontos)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _validador = validador;
        _pontos = pontos;
    }

    public async Task<Result<PontoDetalhe>> ExecutarAsync(
        CriarPontoEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<PontoDetalhe>(ParaErros(validacao));
        }

        return await ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.PontoCriado,
            CamposRegistrados,
            organizacaoId => _pontos.CriarAsync(organizacaoId, entrada, cancellationToken),
            cancellationToken);
    }

    private static IEnumerable<ErroDeValidacao> ParaErros(FluentValidation.Results.ValidationResult validacao)
    {
        return validacao.Errors.Select(erro =>
            new ErroDeValidacao(erro.PropertyName, erro.ErrorMessage));
    }
}

public sealed class AtualizarPontoHandler : ManipuladorDePonto
{
    private const string CamposRegistrados =
        "nome,municipio,endereco,localizacao,status,horarios,tipos";

    private readonly IValidator<AtualizarPontoEntrada> _validador;
    private readonly IPontosDeColeta _pontos;

    public AtualizarPontoHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IValidator<AtualizarPontoEntrada> validador,
        IPontosDeColeta pontos)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _validador = validador;
        _pontos = pontos;
    }

    public async Task<Result<PontoDetalhe>> ExecutarAsync(
        Guid pontoId,
        AtualizarPontoEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<PontoDetalhe>(validacao.Errors.Select(erro =>
                new ErroDeValidacao(erro.PropertyName, erro.ErrorMessage)));
        }

        return await ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.PontoEditado,
            CamposRegistrados,
            organizacaoId => _pontos.AtualizarAsync(
                organizacaoId,
                pontoId,
                entrada,
                cancellationToken),
            cancellationToken);
    }
}

public sealed class AlterarStatusPontoHandler : ManipuladorDePonto
{
    private readonly IValidator<AlterarStatusPontoEntrada> _validador;
    private readonly IPontosDeColeta _pontos;

    public AlterarStatusPontoHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IValidator<AlterarStatusPontoEntrada> validador,
        IPontosDeColeta pontos)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _validador = validador;
        _pontos = pontos;
    }

    public async Task<Result<PontoDetalhe>> ExecutarAsync(
        Guid pontoId,
        AlterarStatusPontoEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<PontoDetalhe>(validacao.Errors.Select(erro =>
                new ErroDeValidacao(erro.PropertyName, erro.ErrorMessage)));
        }

        _ = Enum.TryParse<StatusPonto>(entrada.Status.Trim(), ignoreCase: true, out var status);

        return await ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.PontoStatusAlterado,
            "status",
            organizacaoId => _pontos.AlterarStatusAsync(
                organizacaoId,
                pontoId,
                status,
                cancellationToken),
            cancellationToken);
    }
}

public sealed class RetirarPontoHandler : ManipuladorDePonto
{
    private readonly IPontosDeColeta _pontos;

    public RetirarPontoHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IPontosDeColeta pontos)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _pontos = pontos;
    }

    public Task<Result<PontoDetalhe>> ExecutarAsync(
        Guid pontoId,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        return ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.PontoRetirado,
            "excluido",
            organizacaoId => _pontos.RetirarAsync(organizacaoId, pontoId, cancellationToken),
            cancellationToken);
    }
}

public sealed class ListarPontosHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IPontosDeColeta _pontos;

    public ListarPontosHandler(ITenantContext tenantContext, IPontosDeColeta pontos)
    {
        _tenantContext = tenantContext;
        _pontos = pontos;
    }

    public async Task<Result<IReadOnlyList<PontoResumo>>> ExecutarAsync(
        CancellationToken cancellationToken = default)
    {
        var escopo = EscopoDaOrganizacaoHelper.Obter(_tenantContext);
        if (escopo.IsFailed)
        {
            return Result.Fail<IReadOnlyList<PontoResumo>>(escopo.Errors);
        }

        var pontos = await _pontos.ListarAsync(escopo.Value.OrganizacaoId, cancellationToken);
        return Result.Ok(pontos);
    }
}

public sealed class ObterPontoHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IPontosDeColeta _pontos;

    public ObterPontoHandler(ITenantContext tenantContext, IPontosDeColeta pontos)
    {
        _tenantContext = tenantContext;
        _pontos = pontos;
    }

    public async Task<Result<PontoDetalhe>> ExecutarAsync(
        Guid pontoId,
        CancellationToken cancellationToken = default)
    {
        var escopo = EscopoDaOrganizacaoHelper.Obter(_tenantContext);
        if (escopo.IsFailed)
        {
            return Result.Fail<PontoDetalhe>(escopo.Errors);
        }

        return await _pontos.ObterAsync(escopo.Value.OrganizacaoId, pontoId, cancellationToken);
    }
}

public sealed class ListarOpcoesDePontosHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IPontosDeColeta _pontos;

    public ListarOpcoesDePontosHandler(ITenantContext tenantContext, IPontosDeColeta pontos)
    {
        _tenantContext = tenantContext;
        _pontos = pontos;
    }

    public async Task<Result<IReadOnlyList<OpcaoDePonto>>> ExecutarAsync(
        CancellationToken cancellationToken = default)
    {
        var escopo = EscopoDaOrganizacaoHelper.Obter(_tenantContext);
        if (escopo.IsFailed)
        {
            return Result.Fail<IReadOnlyList<OpcaoDePonto>>(escopo.Errors);
        }

        var opcoes = await _pontos.ListarOpcoesAsync(escopo.Value.OrganizacaoId, cancellationToken);
        return Result.Ok(opcoes);
    }
}

public static class ConversaoDePonto
{
    public static HorarioDeFuncionamento ConverterHorario(HorarioDoPontoEntrada horario)
    {
        _ = Enum.TryParse<DayOfWeek>(horario.DiaDaSemana.Trim(), ignoreCase: true, out var dia);

        var inicio = TimeOnly.ParseExact(
            horario.HoraInicio,
            LimitesDosPontos.FormatoDeHora,
            CultureInfo.InvariantCulture);
        var fim = TimeOnly.ParseExact(
            horario.HoraFim,
            LimitesDosPontos.FormatoDeHora,
            CultureInfo.InvariantCulture);

        return new HorarioDeFuncionamento(dia, inicio, fim);
    }

    public static StatusPonto ConverterStatus(string status)
    {
        _ = Enum.TryParse<StatusPonto>(status.Trim(), ignoreCase: true, out var convertido);
        return convertido;
    }

    public static string FormatarHora(TimeOnly hora)
    {
        return hora.ToString(LimitesDosPontos.FormatoDeHora, CultureInfo.InvariantCulture);
    }
}
