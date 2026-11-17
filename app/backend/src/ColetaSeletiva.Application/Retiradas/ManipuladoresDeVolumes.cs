using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.PerfilDaOrganizacao;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;
using FluentValidation;

namespace ColetaSeletiva.Application.Retiradas;

public sealed class ListarVolumesHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IVolumes _volumes;

    public ListarVolumesHandler(ITenantContext tenantContext, IVolumes volumes)
    {
        _tenantContext = tenantContext;
        _volumes = volumes;
    }

    public async Task<Result<PaginaDeVolumes>> ExecutarAsync(
        FiltroDeVolumes filtro,
        CancellationToken cancellationToken = default)
    {
        var escopo = EscopoDaOrganizacaoHelper.Obter(_tenantContext);
        if (escopo.IsFailed)
        {
            return Result.Fail<PaginaDeVolumes>(escopo.Errors);
        }

        var pagina = await _volumes.ListarAsync(
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

public sealed class RegistrarRecebimentoHandler : ManipuladorDaOrganizacao
{
    private readonly IValidator<RegistrarRecebimentoEntrada> _validador;
    private readonly IVolumes _volumes;

    public RegistrarRecebimentoHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IValidator<RegistrarRecebimentoEntrada> validador,
        IVolumes volumes)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _validador = validador;
        _volumes = volumes;
    }

    public async Task<Result<VolumeResposta>> ExecutarAsync(
        RegistrarRecebimentoEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<VolumeResposta>(ResultadoDeValidacao.ParaErros(validacao));
        }

        return await ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.VolumeRegistrado,
            EntidadesAuditadas.RegistroVolume,
            volume => volume.Id,
            escopo => _volumes.RegistrarRecebimentoAsync(
                escopo.OrganizacaoId,
                escopo.AtorUsuarioId,
                entrada,
                cancellationToken),
            cancellationToken);
    }
}

public sealed class AjustarVolumeHandler : ManipuladorDaOrganizacao
{
    private readonly IValidator<AjustarVolumeEntrada> _validador;
    private readonly IVolumes _volumes;

    public AjustarVolumeHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IValidator<AjustarVolumeEntrada> validador,
        IVolumes volumes)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _validador = validador;
        _volumes = volumes;
    }

    public async Task<Result<VolumeResposta>> ExecutarAsync(
        Guid volumeId,
        AjustarVolumeEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<VolumeResposta>(ResultadoDeValidacao.ParaErros(validacao));
        }

        return await ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.VolumeAjustado,
            EntidadesAuditadas.RegistroVolume,
            volume => volume.Id,
            escopo => _volumes.AjustarAsync(
                escopo.OrganizacaoId,
                escopo.AtorUsuarioId,
                volumeId,
                entrada,
                cancellationToken),
            cancellationToken);
    }
}

public sealed class ObterResumoDoPainelHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IPainelDeVolumes _painel;

    public ObterResumoDoPainelHandler(
        ITenantContext tenantContext,
        IPainelDeVolumes painel)
    {
        _tenantContext = tenantContext;
        _painel = painel;
    }

    public async Task<Result<ResumoDoPainel>> ExecutarAsync(
        FiltroDoPainel filtro,
        CancellationToken cancellationToken = default)
    {
        var escopo = EscopoDaOrganizacaoHelper.Obter(_tenantContext);
        if (escopo.IsFailed)
        {
            return Result.Fail<ResumoDoPainel>(escopo.Errors);
        }

        var resumo = await _painel.ObterResumoAsync(
            escopo.Value.OrganizacaoId,
            filtro,
            cancellationToken);

        return Result.Ok(resumo);
    }
}

public sealed class ObterSerieDoPainelHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IPainelDeVolumes _painel;

    public ObterSerieDoPainelHandler(
        ITenantContext tenantContext,
        IPainelDeVolumes painel)
    {
        _tenantContext = tenantContext;
        _painel = painel;
    }

    public async Task<Result<SerieDoPainel>> ExecutarAsync(
        FiltroDoPainel filtro,
        string agrupamento,
        CancellationToken cancellationToken = default)
    {
        var escopo = EscopoDaOrganizacaoHelper.Obter(_tenantContext);
        if (escopo.IsFailed)
        {
            return Result.Fail<SerieDoPainel>(escopo.Errors);
        }

        return await _painel.ObterSerieAsync(
            escopo.Value.OrganizacaoId,
            filtro,
            agrupamento,
            cancellationToken);
    }
}

public sealed class ExportarVolumesCsvHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IPainelDeVolumes _painel;

    public ExportarVolumesCsvHandler(
        ITenantContext tenantContext,
        IPainelDeVolumes painel)
    {
        _tenantContext = tenantContext;
        _painel = painel;
    }

    public async Task<Result<ArquivoCsvResposta>> ExecutarAsync(
        FiltroDoPainel filtro,
        CancellationToken cancellationToken = default)
    {
        var escopo = EscopoDaOrganizacaoHelper.Obter(_tenantContext);
        if (escopo.IsFailed)
        {
            return Result.Fail<ArquivoCsvResposta>(escopo.Errors);
        }

        var arquivo = await _painel.ExportarCsvAsync(
            escopo.Value.OrganizacaoId,
            filtro,
            cancellationToken);

        return Result.Ok(arquivo);
    }
}

public sealed class ListarNotificacoesHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly INotificacoes _notificacoes;

    public ListarNotificacoesHandler(
        ITenantContext tenantContext,
        INotificacoes notificacoes)
    {
        _tenantContext = tenantContext;
        _notificacoes = notificacoes;
    }

    public async Task<Result<ListaDeNotificacoesResposta>> ExecutarAsync(
        DateTimeOffset? desde,
        bool apenasNaoLidas,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.Autenticado || _tenantContext.UsuarioId is not Guid usuarioId)
        {
            return Result.Fail<ListaDeNotificacoesResposta>(
                new ErroNaoAutenticado("É necessário autenticar."));
        }

        var lista = await _notificacoes.ListarAsync(
            usuarioId,
            desde,
            apenasNaoLidas,
            cancellationToken);

        return Result.Ok(lista);
    }
}

public sealed class MarcarNotificacaoLidaHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly INotificacoes _notificacoes;

    public MarcarNotificacaoLidaHandler(
        ITenantContext tenantContext,
        INotificacoes notificacoes)
    {
        _tenantContext = tenantContext;
        _notificacoes = notificacoes;
    }

    public async Task<Result<NotificacaoResposta>> ExecutarAsync(
        Guid notificacaoId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.Autenticado || _tenantContext.UsuarioId is not Guid usuarioId)
        {
            return Result.Fail<NotificacaoResposta>(
                new ErroNaoAutenticado("É necessário autenticar."));
        }

        return await _notificacoes.MarcarComoLidaAsync(
            usuarioId,
            notificacaoId,
            cancellationToken);
    }
}

public sealed class MarcarTodasNotificacoesLidasHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly INotificacoes _notificacoes;

    public MarcarTodasNotificacoesLidasHandler(
        ITenantContext tenantContext,
        INotificacoes notificacoes)
    {
        _tenantContext = tenantContext;
        _notificacoes = notificacoes;
    }

    public async Task<Result<int>> ExecutarAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.Autenticado || _tenantContext.UsuarioId is not Guid usuarioId)
        {
            return Result.Fail<int>(new ErroNaoAutenticado("É necessário autenticar."));
        }

        return await _notificacoes.MarcarTodasComoLidasAsync(usuarioId, cancellationToken);
    }
}
