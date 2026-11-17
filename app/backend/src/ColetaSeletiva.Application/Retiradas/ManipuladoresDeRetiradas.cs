using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.PerfilDaOrganizacao;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;
using FluentValidation;

namespace ColetaSeletiva.Application.Retiradas;

internal static class ResultadoDeValidacao
{
    public static IEnumerable<ErroDeValidacao> ParaErros(
        FluentValidation.Results.ValidationResult validacao)
    {
        return validacao.Errors.Select(erro =>
            new ErroDeValidacao(erro.PropertyName, erro.ErrorMessage));
    }

    public static int NormalizarPagina(int pagina) => pagina < 1 ? 1 : pagina;

    public static int NormalizarTamanhoPagina(int tamanhoPagina) =>
        tamanhoPagina switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => tamanhoPagina,
        };
}

/// <summary>
/// Base dos handlers da organização para retiradas, disponibilidade e volumes:
/// escopo derivado do token, transação e auditoria (RF-03, RF-10–RF-16; RNF-01, RNF-05).
/// </summary>
public abstract class ManipuladorDaOrganizacao
{
    private readonly ITenantContext _tenantContext;
    private readonly IRegistroDeAuditoria _auditoria;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly TimeProvider _relogio;

    protected ManipuladorDaOrganizacao(
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

    protected Result<EscopoDaOrganizacao> ObterEscopo() =>
        EscopoDaOrganizacaoHelper.Obter(_tenantContext);

    protected async Task<Result<T>> ExecutarComAuditoriaAsync<T>(
        Guid correlacaoId,
        string acao,
        string entidade,
        Func<T, Guid?> obterEntidadeId,
        Func<EscopoDaOrganizacao, Task<Result<T>>> operacao,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operacao);
        ArgumentNullException.ThrowIfNull(obterEntidadeId);

        if (correlacaoId == Guid.Empty)
        {
            throw new ArgumentException(
                "A correlação da operação não pode ser vazia.",
                nameof(correlacaoId));
        }

        var escopo = ObterEscopo();
        if (escopo.IsFailed)
        {
            return Result.Fail<T>(escopo.Errors);
        }

        await using var transacao = await _unidadeDeTrabalho.IniciarAsync(cancellationToken);

        try
        {
            var resultado = await operacao(escopo.Value);
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
                    Entidade: entidade,
                    EntidadeId: obterEntidadeId(resultado.Value),
                    CorrelacaoId: correlacaoId,
                    OcorridoEmUtc: _relogio.GetUtcNow()),
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

    protected async Task<Result> ExecutarComAuditoriaAsync(
        Guid correlacaoId,
        string acao,
        string entidade,
        Guid? entidadeId,
        Func<EscopoDaOrganizacao, Task<Result>> operacao,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operacao);

        if (correlacaoId == Guid.Empty)
        {
            throw new ArgumentException(
                "A correlação da operação não pode ser vazia.",
                nameof(correlacaoId));
        }

        var escopo = ObterEscopo();
        if (escopo.IsFailed)
        {
            return Result.Fail(escopo.Errors);
        }

        await using var transacao = await _unidadeDeTrabalho.IniciarAsync(cancellationToken);

        try
        {
            var resultado = await operacao(escopo.Value);
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
                    Entidade: entidade,
                    EntidadeId: entidadeId,
                    CorrelacaoId: correlacaoId,
                    OcorridoEmUtc: _relogio.GetUtcNow()),
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

/// <summary>
/// Base dos handlers do cidadão: escopo próprio, transação e auditoria sem organização.
/// </summary>
public abstract class ManipuladorDoCidadao
{
    private readonly IRegistroDeAuditoria _auditoria;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly TimeProvider _relogio;
    private readonly ITenantContext _tenantContext;

    protected ManipuladorDoCidadao(
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

    protected Result<EscopoDoCidadao> ObterEscopo() =>
        EscopoDoCidadaoHelper.Obter(_tenantContext);

    protected async Task<Result<T>> ExecutarComAuditoriaAsync<T>(
        Guid correlacaoId,
        string acao,
        string entidade,
        Func<T, Guid?> obterEntidadeId,
        Func<EscopoDoCidadao, Task<Result<T>>> operacao,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operacao);
        ArgumentNullException.ThrowIfNull(obterEntidadeId);

        if (correlacaoId == Guid.Empty)
        {
            throw new ArgumentException(
                "A correlação da operação não pode ser vazia.",
                nameof(correlacaoId));
        }

        var escopo = ObterEscopo();
        if (escopo.IsFailed)
        {
            return Result.Fail<T>(escopo.Errors);
        }

        await using var transacao = await _unidadeDeTrabalho.IniciarAsync(cancellationToken);

        try
        {
            var resultado = await operacao(escopo.Value);
            if (resultado.IsFailed)
            {
                await transacao.RollbackAsync(cancellationToken);
                return resultado;
            }

            await _auditoria.RegistrarAsync(
                new EventoDeAuditoria(
                    OrganizacaoId: null,
                    AtorUsuarioId: escopo.Value.AtorUsuarioId,
                    Acao: acao,
                    Entidade: entidade,
                    EntidadeId: obterEntidadeId(resultado.Value),
                    CorrelacaoId: correlacaoId,
                    OcorridoEmUtc: _relogio.GetUtcNow()),
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

public sealed class ListarOrganizacoesElegiveisHandler
{
    private readonly IRetiradas _retiradas;

    public ListarOrganizacoesElegiveisHandler(IRetiradas retiradas)
    {
        _retiradas = retiradas;
    }

    public Task<IReadOnlyList<OrganizacaoParaRetiradaResposta>> ExecutarAsync(
        int municipioId,
        CancellationToken cancellationToken = default)
    {
        return _retiradas.ListarOrganizacoesElegiveisAsync(municipioId, cancellationToken);
    }
}

public sealed class ListarJanelasDeRetiradaHandler
{
    private readonly IRetiradas _retiradas;

    public ListarJanelasDeRetiradaHandler(IRetiradas retiradas)
    {
        _retiradas = retiradas;
    }

    public async Task<Result<IReadOnlyList<JanelaDeRetiradaResposta>>> ExecutarAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken = default)
    {
        if (organizacaoId == Guid.Empty)
        {
            return Result.Fail<IReadOnlyList<JanelaDeRetiradaResposta>>(
                new ErroDeValidacao("organizacaoId", "Informe a organização."));
        }

        return await _retiradas.ListarJanelasAsync(organizacaoId, cancellationToken);
    }
}

public sealed class CriarRetiradaHandler : ManipuladorDoCidadao
{
    private readonly IValidator<CriarRetiradaEntrada> _validador;
    private readonly IRetiradas _retiradas;

    public CriarRetiradaHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IValidator<CriarRetiradaEntrada> validador,
        IRetiradas retiradas)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _validador = validador;
        _retiradas = retiradas;
    }

    public async Task<Result<RetiradaDetalhe>> ExecutarAsync(
        CriarRetiradaEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<RetiradaDetalhe>(ResultadoDeValidacao.ParaErros(validacao));
        }

        return await ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.SolicitacaoCriada,
            EntidadesAuditadas.SolicitacaoRetirada,
            detalhe => detalhe.Id,
            escopo => _retiradas.CriarAsync(escopo.CidadaoId, entrada, cancellationToken),
            cancellationToken);
    }
}

public sealed class ListarMinhasRetiradasHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IRetiradas _retiradas;

    public ListarMinhasRetiradasHandler(ITenantContext tenantContext, IRetiradas retiradas)
    {
        _tenantContext = tenantContext;
        _retiradas = retiradas;
    }

    public async Task<Result<PaginaDeRetiradas>> ExecutarAsync(
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default)
    {
        var escopo = EscopoDoCidadaoHelper.Obter(_tenantContext);
        if (escopo.IsFailed)
        {
            return Result.Fail<PaginaDeRetiradas>(escopo.Errors);
        }

        var resultado = await _retiradas.ListarMinhasAsync(
            escopo.Value.CidadaoId,
            ResultadoDeValidacao.NormalizarPagina(pagina),
            ResultadoDeValidacao.NormalizarTamanhoPagina(tamanhoPagina),
            cancellationToken);

        return Result.Ok(resultado);
    }
}

public sealed class ObterMinhaRetiradaHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IRetiradas _retiradas;

    public ObterMinhaRetiradaHandler(ITenantContext tenantContext, IRetiradas retiradas)
    {
        _tenantContext = tenantContext;
        _retiradas = retiradas;
    }

    public async Task<Result<RetiradaDetalhe>> ExecutarAsync(
        Guid solicitacaoId,
        CancellationToken cancellationToken = default)
    {
        var escopo = EscopoDoCidadaoHelper.Obter(_tenantContext);
        if (escopo.IsFailed)
        {
            return Result.Fail<RetiradaDetalhe>(escopo.Errors);
        }

        return await _retiradas.ObterMinhaAsync(
            escopo.Value.CidadaoId,
            solicitacaoId,
            cancellationToken);
    }
}

public sealed class CancelarRetiradaHandler : ManipuladorDoCidadao
{
    private readonly IRetiradas _retiradas;

    public CancelarRetiradaHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IRetiradas retiradas)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _retiradas = retiradas;
    }

    public Task<Result<RetiradaDetalhe>> ExecutarAsync(
        Guid solicitacaoId,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        return ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.SolicitacaoCancelada,
            EntidadesAuditadas.SolicitacaoRetirada,
            detalhe => detalhe.Id,
            escopo => _retiradas.CancelarAsync(
                escopo.CidadaoId,
                solicitacaoId,
                cancellationToken),
            cancellationToken);
    }
}

public sealed class ListarRetiradasHandler
{
    private readonly ManipuladorDaOrganizacaoLeitura _leitura;
    private readonly IRetiradas _retiradas;

    public ListarRetiradasHandler(
        ITenantContext tenantContext,
        IRetiradas retiradas)
    {
        _leitura = new ManipuladorDaOrganizacaoLeitura(tenantContext);
        _retiradas = retiradas;
    }

    public async Task<Result<PaginaDeRetiradas>> ExecutarAsync(
        FiltroDeRetiradasDaOrganizacao filtro,
        CancellationToken cancellationToken = default)
    {
        var escopo = _leitura.ObterEscopo();
        if (escopo.IsFailed)
        {
            return Result.Fail<PaginaDeRetiradas>(escopo.Errors);
        }

        var pagina = await _retiradas.ListarAsync(
            escopo.Value.OrganizacaoId,
            filtro with
            {
                Pagina = ResultadoDeValidacao.NormalizarPagina(filtro.Pagina),
                TamanhoPagina = ResultadoDeValidacao.NormalizarTamanhoPagina(filtro.TamanhoPagina),
            },
            cancellationToken);

        return Result.Ok(pagina);
    }
}

public sealed class ObterRetiradaHandler
{
    private readonly ManipuladorDaOrganizacaoLeitura _leitura;
    private readonly IRetiradas _retiradas;

    public ObterRetiradaHandler(ITenantContext tenantContext, IRetiradas retiradas)
    {
        _leitura = new ManipuladorDaOrganizacaoLeitura(tenantContext);
        _retiradas = retiradas;
    }

    public async Task<Result<RetiradaDetalhe>> ExecutarAsync(
        Guid solicitacaoId,
        CancellationToken cancellationToken = default)
    {
        var escopo = _leitura.ObterEscopo();
        if (escopo.IsFailed)
        {
            return Result.Fail<RetiradaDetalhe>(escopo.Errors);
        }

        return await _retiradas.ObterAsync(
            escopo.Value.OrganizacaoId,
            solicitacaoId,
            cancellationToken);
    }
}

public sealed class AgendarRetiradaHandler : ManipuladorDaOrganizacao
{
    private readonly IValidator<AgendarRetiradaEntrada> _validador;
    private readonly IRetiradas _retiradas;

    public AgendarRetiradaHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IValidator<AgendarRetiradaEntrada> validador,
        IRetiradas retiradas)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _validador = validador;
        _retiradas = retiradas;
    }

    public async Task<Result<RetiradaDetalhe>> ExecutarAsync(
        Guid solicitacaoId,
        AgendarRetiradaEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<RetiradaDetalhe>(ResultadoDeValidacao.ParaErros(validacao));
        }

        return await ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.RetiradaAgendada,
            EntidadesAuditadas.SolicitacaoRetirada,
            detalhe => detalhe.Id,
            escopo => _retiradas.AgendarAsync(
                escopo.OrganizacaoId,
                escopo.AtorUsuarioId,
                solicitacaoId,
                entrada,
                cancellationToken),
            cancellationToken);
    }
}

public sealed class ReagendarRetiradaHandler : ManipuladorDaOrganizacao
{
    private readonly IValidator<ReagendarRetiradaEntrada> _validador;
    private readonly IRetiradas _retiradas;

    public ReagendarRetiradaHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IValidator<ReagendarRetiradaEntrada> validador,
        IRetiradas retiradas)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _validador = validador;
        _retiradas = retiradas;
    }

    public async Task<Result<RetiradaDetalhe>> ExecutarAsync(
        Guid solicitacaoId,
        ReagendarRetiradaEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<RetiradaDetalhe>(ResultadoDeValidacao.ParaErros(validacao));
        }

        return await ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.RetiradaReagendada,
            EntidadesAuditadas.SolicitacaoRetirada,
            detalhe => detalhe.Id,
            escopo => _retiradas.ReagendarAsync(
                escopo.OrganizacaoId,
                escopo.AtorUsuarioId,
                solicitacaoId,
                entrada,
                cancellationToken),
            cancellationToken);
    }
}

public sealed class RecusarRetiradaHandler : ManipuladorDaOrganizacao
{
    private readonly IValidator<RecusarRetiradaEntrada> _validador;
    private readonly IRetiradas _retiradas;

    public RecusarRetiradaHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IValidator<RecusarRetiradaEntrada> validador,
        IRetiradas retiradas)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _validador = validador;
        _retiradas = retiradas;
    }

    public async Task<Result<RetiradaDetalhe>> ExecutarAsync(
        Guid solicitacaoId,
        RecusarRetiradaEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<RetiradaDetalhe>(ResultadoDeValidacao.ParaErros(validacao));
        }

        return await ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.RetiradaRecusada,
            EntidadesAuditadas.SolicitacaoRetirada,
            detalhe => detalhe.Id,
            escopo => _retiradas.RecusarAsync(
                escopo.OrganizacaoId,
                escopo.AtorUsuarioId,
                solicitacaoId,
                entrada,
                cancellationToken),
            cancellationToken);
    }
}

public sealed class ExecutarRetiradaHandler : ManipuladorDaOrganizacao
{
    private readonly IValidator<ExecutarRetiradaEntrada> _validador;
    private readonly IRetiradas _retiradas;

    public ExecutarRetiradaHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IValidator<ExecutarRetiradaEntrada> validador,
        IRetiradas retiradas)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _validador = validador;
        _retiradas = retiradas;
    }

    public async Task<Result<RetiradaDetalhe>> ExecutarAsync(
        Guid solicitacaoId,
        ExecutarRetiradaEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<RetiradaDetalhe>(ResultadoDeValidacao.ParaErros(validacao));
        }

        return await ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.RetiradaExecutada,
            EntidadesAuditadas.SolicitacaoRetirada,
            detalhe => detalhe.Id,
            escopo => _retiradas.ExecutarAsync(
                escopo.OrganizacaoId,
                escopo.AtorUsuarioId,
                solicitacaoId,
                entrada,
                cancellationToken),
            cancellationToken);
    }
}

public sealed class MarcarRetiradaNaoRealizadaHandler : ManipuladorDaOrganizacao
{
    private readonly IValidator<NaoRealizadaEntrada> _validador;
    private readonly IRetiradas _retiradas;

    public MarcarRetiradaNaoRealizadaHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IValidator<NaoRealizadaEntrada> validador,
        IRetiradas retiradas)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _validador = validador;
        _retiradas = retiradas;
    }

    public async Task<Result<RetiradaDetalhe>> ExecutarAsync(
        Guid solicitacaoId,
        NaoRealizadaEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<RetiradaDetalhe>(ResultadoDeValidacao.ParaErros(validacao));
        }

        return await ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.RetiradaNaoRealizada,
            EntidadesAuditadas.SolicitacaoRetirada,
            detalhe => detalhe.Id,
            escopo => _retiradas.MarcarNaoRealizadaAsync(
                escopo.OrganizacaoId,
                escopo.AtorUsuarioId,
                solicitacaoId,
                entrada,
                cancellationToken),
            cancellationToken);
    }
}

public sealed class ObterDisponibilidadeHandler
{
    private readonly ManipuladorDaOrganizacaoLeitura _leitura;
    private readonly IDisponibilidadeDeRetirada _disponibilidade;

    public ObterDisponibilidadeHandler(
        ITenantContext tenantContext,
        IDisponibilidadeDeRetirada disponibilidade)
    {
        _leitura = new ManipuladorDaOrganizacaoLeitura(tenantContext);
        _disponibilidade = disponibilidade;
    }

    public async Task<Result<IReadOnlyList<DisponibilidadeDiaResposta>>> ExecutarAsync(
        CancellationToken cancellationToken = default)
    {
        var escopo = _leitura.ObterEscopo();
        if (escopo.IsFailed)
        {
            return Result.Fail<IReadOnlyList<DisponibilidadeDiaResposta>>(escopo.Errors);
        }

        var dias = await _disponibilidade.ObterAsync(
            escopo.Value.OrganizacaoId,
            cancellationToken);

        return Result.Ok(dias);
    }
}

public sealed class DefinirDisponibilidadeHandler : ManipuladorDaOrganizacao
{
    private readonly IValidator<DisponibilidadeSemanaEntrada> _validador;
    private readonly IDisponibilidadeDeRetirada _disponibilidade;

    public DefinirDisponibilidadeHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IValidator<DisponibilidadeSemanaEntrada> validador,
        IDisponibilidadeDeRetirada disponibilidade)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _validador = validador;
        _disponibilidade = disponibilidade;
    }

    public async Task<Result<IReadOnlyList<DisponibilidadeDiaResposta>>> ExecutarAsync(
        DisponibilidadeSemanaEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<IReadOnlyList<DisponibilidadeDiaResposta>>(
                ResultadoDeValidacao.ParaErros(validacao));
        }

        return await ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.DisponibilidadeDefinida,
            EntidadesAuditadas.DisponibilidadeRetirada,
            _ => null,
            escopo => _disponibilidade.DefinirAsync(
                escopo.OrganizacaoId,
                entrada.Dias,
                cancellationToken),
            cancellationToken);
    }
}

public sealed class ListarBloqueiosHandler
{
    private readonly ManipuladorDaOrganizacaoLeitura _leitura;
    private readonly IDisponibilidadeDeRetirada _disponibilidade;

    public ListarBloqueiosHandler(
        ITenantContext tenantContext,
        IDisponibilidadeDeRetirada disponibilidade)
    {
        _leitura = new ManipuladorDaOrganizacaoLeitura(tenantContext);
        _disponibilidade = disponibilidade;
    }

    public async Task<Result<IReadOnlyList<BloqueioDeDataResposta>>> ExecutarAsync(
        CancellationToken cancellationToken = default)
    {
        var escopo = _leitura.ObterEscopo();
        if (escopo.IsFailed)
        {
            return Result.Fail<IReadOnlyList<BloqueioDeDataResposta>>(escopo.Errors);
        }

        var bloqueios = await _disponibilidade.ListarBloqueiosAsync(
            escopo.Value.OrganizacaoId,
            cancellationToken);

        return Result.Ok(bloqueios);
    }
}

public sealed class CriarBloqueioHandler : ManipuladorDaOrganizacao
{
    private readonly IDisponibilidadeDeRetirada _disponibilidade;

    public CriarBloqueioHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IDisponibilidadeDeRetirada disponibilidade)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _disponibilidade = disponibilidade;
    }

    public Task<Result<BloqueioDeDataResposta>> ExecutarAsync(
        BloqueioDeDataEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        if (entrada.Data == default)
        {
            return Task.FromResult(Result.Fail<BloqueioDeDataResposta>(
                new ErroDeValidacao("data", "Informe a data do bloqueio.")));
        }

        return ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.DataBloqueada,
            EntidadesAuditadas.BloqueioData,
            bloqueio => bloqueio.Id,
            escopo => _disponibilidade.CriarBloqueioAsync(
                escopo.OrganizacaoId,
                escopo.AtorUsuarioId,
                entrada,
                cancellationToken),
            cancellationToken);
    }
}

public sealed class RemoverBloqueioHandler : ManipuladorDaOrganizacao
{
    private readonly IDisponibilidadeDeRetirada _disponibilidade;

    public RemoverBloqueioHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IDisponibilidadeDeRetirada disponibilidade)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _disponibilidade = disponibilidade;
    }

    public Task<Result> ExecutarAsync(
        Guid bloqueioId,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        return ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.DataDesbloqueada,
            EntidadesAuditadas.BloqueioData,
            bloqueioId,
            escopo => _disponibilidade.RemoverBloqueioAsync(
                escopo.OrganizacaoId,
                bloqueioId,
                cancellationToken),
            cancellationToken);
    }
}

internal sealed class ManipuladorDaOrganizacaoLeitura
{
    private readonly ITenantContext _tenantContext;

    public ManipuladorDaOrganizacaoLeitura(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public Result<EscopoDaOrganizacao> ObterEscopo() =>
        EscopoDaOrganizacaoHelper.Obter(_tenantContext);
}
