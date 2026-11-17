using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Retiradas;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Retiradas;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.Infrastructure.Retiradas;

internal static class FusoDaOrganizacao
{
    private static readonly TimeZoneInfo Padrao = TimeZoneInfo.FindSystemTimeZoneById(
        Organizacao.FusoPadrao);

    public static TimeZoneInfo Obter(Organizacao organizacao)
    {
        ArgumentNullException.ThrowIfNull(organizacao);

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(organizacao.Fuso);
        }
        catch (TimeZoneNotFoundException)
        {
            return Padrao;
        }
        catch (InvalidTimeZoneException)
        {
            return Padrao;
        }
    }

    public static DateOnly Hoje(TimeZoneInfo fuso, DateTimeOffset agora) =>
        PoliticaDeRetirada.HojeNoFuso(agora, fuso);

    public static DateOnly DataLocal(TimeZoneInfo fuso, DateTimeOffset instante) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instante, fuso).DateTime);
}

internal sealed class RetiradasService : IRetiradas
{
    private const int LimiteDeDiasParaRelatorio = 366;

    private readonly ColetaSeletivaDbContext _dbContext;
    private readonly TimeProvider _relogio;

    public RetiradasService(ColetaSeletivaDbContext dbContext, TimeProvider relogio)
    {
        _dbContext = dbContext;
        _relogio = relogio;
    }

    public async Task<IReadOnlyList<OrganizacaoParaRetiradaResposta>>
        ListarOrganizacoesElegiveisAsync(
            int municipioId,
            CancellationToken cancellationToken = default)
    {
        if (municipioId <= 0)
        {
            return [];
        }

        var organizacoes = await (
                from vinculo in _dbContext.OrganizacaoMunicipios.IgnoreQueryFilters()
                join organizacao in _dbContext.Organizacoes.IgnoreQueryFilters()
                    on vinculo.OrganizacaoId equals organizacao.Id
                where vinculo.MunicipioId == municipioId && organizacao.Publicada
                select organizacao)
            .AsNoTracking()
            .Distinct()
            .OrderBy(organizacao => organizacao.Nome)
            .ToListAsync(cancellationToken);

        if (organizacoes.Count == 0)
        {
            return [];
        }

        var ids = organizacoes.Select(organizacao => organizacao.Id).ToList();
        var tipos = await (
                from vinculo in _dbContext.OrganizacaoTipos.IgnoreQueryFilters()
                join tipo in _dbContext.TiposResiduo.AsNoTracking()
                    on vinculo.TipoResiduoId equals tipo.Id
                where ids.Contains(vinculo.OrganizacaoId)
                    && vinculo.AceitaRetiradaDomiciliar
                orderby tipo.Nome, tipo.Id
                select new
                {
                    vinculo.OrganizacaoId,
                    Tipo = tipo,
                })
            .ToListAsync(cancellationToken);

        return organizacoes
            .Select(organizacao =>
            {
                var tiposDaOrganizacao = tipos
                    .Where(item => item.OrganizacaoId == organizacao.Id)
                    .Select(item => new TipoDomiciliarResposta(
                        item.Tipo.Id,
                        item.Tipo.Codigo,
                        item.Tipo.Nome,
                        item.Tipo.Categoria.ToString(),
                        item.Tipo.Unidade.ToString()))
                    .ToList();

                return new OrganizacaoParaRetiradaResposta(
                    organizacao.Id,
                    organizacao.Nome,
                    organizacao.Tipo.ToString(),
                    organizacao.ContatoEmail,
                    organizacao.ContatoTelefone,
                    tiposDaOrganizacao);
            })
            .Where(organizacao => organizacao.Tipos.Count > 0)
            .ToList();
    }

    public async Task<Result<IReadOnlyList<JanelaDeRetiradaResposta>>> ListarJanelasAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken = default)
    {
        var organizacao = await ObterOrganizacaoPublicadaAsync(
            organizacaoId,
            cancellationToken);

        if (organizacao is null)
        {
            return Result.Fail<IReadOnlyList<JanelaDeRetiradaResposta>>(
                new ErroNaoEncontrado("Organização não encontrada."));
        }

        var agora = _relogio.GetUtcNow();
        var fuso = FusoDaOrganizacao.Obter(organizacao);
        var hoje = FusoDaOrganizacao.Hoje(fuso, agora);
        var inicio = hoje.AddDays(PoliticaDeRetirada.DiasDeAntecedenciaMinima);
        var fim = hoje.AddDays(PoliticaDeRetirada.DiasDeHorizonteMaximo);

        var capacidades = await _dbContext
            .DisponibilidadesDeRetirada
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(disponibilidade => disponibilidade.OrganizacaoId == organizacaoId)
            .ToListAsync(cancellationToken);

        if (capacidades.Count == 0)
        {
            return Result.Ok<IReadOnlyList<JanelaDeRetiradaResposta>>([]);
        }

        var bloqueios = await _dbContext
            .BloqueiosDeData
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(bloqueio => bloqueio.OrganizacaoId == organizacaoId
                && bloqueio.DataLocal >= inicio
                && bloqueio.DataLocal <= fim)
            .Select(bloqueio => bloqueio.DataLocal)
            .ToListAsync(cancellationToken);

        var bloqueadas = bloqueios.ToHashSet();
        var periodos = await ObterPeriodosAsync(organizacaoId, cancellationToken);

        var ocupacao = await _dbContext
            .SolicitacoesDeRetirada
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(solicitacao => solicitacao.OrganizacaoId == organizacaoId
                && solicitacao.DataLocal >= inicio
                && solicitacao.DataLocal <= fim
                && (solicitacao.Estado == EstadoSolicitacao.Solicitada
                    || solicitacao.Estado == EstadoSolicitacao.Agendada))
            .GroupBy(solicitacao => new { solicitacao.DataLocal, solicitacao.Periodo })
            .Select(grupo => new
            {
                grupo.Key.DataLocal,
                grupo.Key.Periodo,
                Total = grupo.Count(),
            })
            .ToListAsync(cancellationToken);

        var ocupacaoPorJanela = ocupacao.ToDictionary(
            item => (item.DataLocal, item.Periodo),
            item => item.Total);

        var resposta = new List<JanelaDeRetiradaResposta>();

        for (var data = inicio; data <= fim; data = data.AddDays(1))
        {
            if (bloqueadas.Contains(data))
            {
                continue;
            }

            foreach (var periodo in new[] { PeriodoDeRetirada.Manha, PeriodoDeRetirada.Tarde })
            {
                var capacidade = capacidades.FirstOrDefault(item =>
                    item.OrganizacaoId == organizacaoId
                    && item.DiaDaSemana == data.DayOfWeek
                    && item.Periodo == periodo);

                if (capacidade is null || capacidade.Capacidade <= 0)
                {
                    continue;
                }

                var usados = ocupacaoPorJanela.GetValueOrDefault((data, periodo));
                if (usados >= capacidade.Capacidade)
                {
                    continue;
                }

                var (horaInicio, horaFim) = periodos.TryGetValue(periodo, out var configurado)
                    ? configurado
                    : (PoliticaDeRetirada.HoraInicioPadrao(periodo),
                        PoliticaDeRetirada.HoraFimPadrao(periodo));

                resposta.Add(new JanelaDeRetiradaResposta(
                    data,
                    periodo.ToString(),
                    horaInicio.ToString("HH:mm"),
                    horaFim.ToString("HH:mm"),
                    capacidade.Capacidade,
                    usados,
                    capacidade.Capacidade - usados));
            }
        }

        return Result.Ok<IReadOnlyList<JanelaDeRetiradaResposta>>(resposta);
    }

    public async Task<Result<RetiradaDetalhe>> CriarAsync(
        Guid cidadaoId,
        CriarRetiradaEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        var organizacao = await ObterOrganizacaoPublicadaAsync(
            entrada.OrganizacaoId,
            cancellationToken);

        if (organizacao is null)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroNaoEncontrado("Organização não encontrada."));
        }

        var municipio = await _dbContext
            .Municipios
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == entrada.MunicipioId, cancellationToken);

        if (municipio is null)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroDeValidacao("municipioId", "Informe um município válido."));
        }

        var atendeMunicipio = await _dbContext
            .OrganizacaoMunicipios
            .IgnoreQueryFilters()
            .AnyAsync(
                vinculo => vinculo.OrganizacaoId == organizacao.Id
                    && vinculo.MunicipioId == entrada.MunicipioId,
                cancellationToken);

        if (!atendeMunicipio)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroDeValidacao(
                    "municipioId",
                    "A organização não atende o município informado."));
        }

        var tipoIds = entrada.Itens.Select(item => item.TipoResiduoId).ToList();
        var habilitados = await (
                from vinculo in _dbContext.OrganizacaoTipos.IgnoreQueryFilters()
                where vinculo.OrganizacaoId == organizacao.Id
                    && vinculo.AceitaRetiradaDomiciliar
                    && tipoIds.Contains(vinculo.TipoResiduoId)
                select vinculo.TipoResiduoId)
            .ToListAsync(cancellationToken);

        if (habilitados.Count != tipoIds.Distinct().Count())
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroDeValidacao(
                    "itens",
                    "Todos os tipos devem aceitar retirada domiciliar nessa organização."));
        }

        var cidadao = await _dbContext
            .Usuarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                usuario => usuario.Id == cidadaoId && usuario.OrganizacaoId == null,
                cancellationToken);

        if (cidadao is null)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroNaoEncontrado("Cidadão não encontrado."));
        }

        if (!Enum.TryParse<PeriodoDeRetirada>(
                entrada.Periodo,
                ignoreCase: true,
                out var periodo))
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroDeValidacao("periodo", "Informe um período válido."));
        }

        var agora = _relogio.GetUtcNow();
        var fuso = FusoDaOrganizacao.Obter(organizacao);
        var hoje = FusoDaOrganizacao.Hoje(fuso, agora);

        if (!PoliticaDeRetirada.DentroDoHorizonte(hoje, entrada.Data))
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroDeValidacao(
                    "data",
                    $"A data deve estar entre amanhã e {PoliticaDeRetirada.DiasDeHorizonteMaximo} dias."));
        }

        await TravarOrganizacaoAsync(organizacao.Id, cancellationToken);

        var capacidade = await _dbContext
            .DisponibilidadesDeRetirada
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.OrganizacaoId == organizacao.Id
                    && item.DiaDaSemana == entrada.Data.DayOfWeek
                    && item.Periodo == periodo,
                cancellationToken);

        if (capacidade is null || capacidade.Capacidade <= 0)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroDeConflito(
                    CodigosDeErro.JanelaIndisponivel,
                    "Não há disponibilidade para a data e o período informados."));
        }

        var bloqueada = await _dbContext
            .BloqueiosDeData
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                bloqueio => bloqueio.OrganizacaoId == organizacao.Id
                    && bloqueio.DataLocal == entrada.Data,
                cancellationToken);

        if (bloqueada)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroDeConflito(
                    CodigosDeErro.DataBloqueada,
                    "A data informada está bloqueada para novas solicitações."));
        }

        var abertas = await _dbContext
            .SolicitacoesDeRetirada
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(
                solicitacao => solicitacao.CidadaoId == cidadaoId
                    && solicitacao.OrganizacaoId == organizacao.Id
                    && (solicitacao.Estado == EstadoSolicitacao.Solicitada
                        || solicitacao.Estado == EstadoSolicitacao.Agendada),
                cancellationToken);

        if (abertas >= organizacao.LimiteSolicitacoesAbertas)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroDeConflito(
                    CodigosDeErro.LimiteDeSolicitacoes,
                    "Você atingiu o limite de solicitações abertas nessa organização."));
        }

        var ocupacao = await _dbContext
            .SolicitacoesDeRetirada
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(
                solicitacao => solicitacao.OrganizacaoId == organizacao.Id
                    && solicitacao.DataLocal == entrada.Data
                    && solicitacao.Periodo == periodo
                    && (solicitacao.Estado == EstadoSolicitacao.Solicitada
                        || solicitacao.Estado == EstadoSolicitacao.Agendada),
                cancellationToken);

        if (ocupacao >= capacidade.Capacidade)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroDeConflito(
                    CodigosDeErro.CapacidadeEsgotada,
                    "A janela escolhida não possui mais vagas."));
        }

        await GarantirJanelaAsync(organizacao.Id, entrada.Data, periodo, agora, cancellationToken);

        var solicitacao = SolicitacaoDeRetirada.Criar(
            organizacao.Id,
            cidadaoId,
            cidadao.Nome,
            cidadao.Telefone,
            municipio.Uf,
            municipio.Id,
            municipio.Nome,
            entrada.Bairro,
            entrada.Rua,
            entrada.Numero,
            entrada.Complemento,
            entrada.Cep,
            entrada.PontoReferencia,
            entrada.Observacoes,
            entrada.Data,
            periodo,
            entrada.Itens
                .Select(item => (item.TipoResiduoId, item.QuantidadeEstimada))
                .ToList(),
            cidadaoId,
            agora);

        _dbContext.SolicitacoesDeRetirada.Add(solicitacao);

        await AdicionarNotificacoesDaEquipeAsync(
            organizacao.Id,
            solicitacao.Id,
            TipoDeNotificacao.SolicitacaoCriada,
            "Nova solicitação de retirada",
            $"Uma nova solicitação de retirada foi registrada para {entrada.Data:dd/MM/yyyy} ({periodo}).",
            agora,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ObterMinhaAsync(cidadaoId, solicitacao.Id, cancellationToken);
    }

    public async Task<PaginaDeRetiradas> ListarMinhasAsync(
        Guid cidadaoId,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default)
    {
        var consulta = _dbContext
            .SolicitacoesDeRetirada
            .AsNoTracking()
            .Where(solicitacao => solicitacao.CidadaoId == cidadaoId);

        var total = await consulta.CountAsync(cancellationToken);
        var solicitacoes = await consulta
            .OrderByDescending(solicitacao => solicitacao.CriadoEmUtc)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        var nomes = await ObterNomesDeOrganizacoesAsync(
            solicitacoes.Select(solicitacao => solicitacao.OrganizacaoId),
            cancellationToken);

        var itens = solicitacoes
            .Select(solicitacao => new RetiradaResumo(
                solicitacao.Id,
                solicitacao.Estado.ToString(),
                solicitacao.DataLocal,
                solicitacao.Periodo.ToString(),
                solicitacao.OrganizacaoId,
                nomes.GetValueOrDefault(solicitacao.OrganizacaoId, "Organização"),
                solicitacao.MunicipioId,
                solicitacao.MunicipioNome,
                solicitacao.Bairro,
                solicitacao.CriadoEmUtc))
            .ToList();

        return new PaginaDeRetiradas(itens, pagina, tamanhoPagina, total);
    }

    public async Task<Result<RetiradaDetalhe>> ObterMinhaAsync(
        Guid cidadaoId,
        Guid solicitacaoId,
        CancellationToken cancellationToken = default)
    {
        var solicitacao = await _dbContext
            .SolicitacoesDeRetirada
            .AsNoTracking()
            .Include(item => item.Itens)
            .Include(item => item.Historico)
            .FirstOrDefaultAsync(
                item => item.Id == solicitacaoId && item.CidadaoId == cidadaoId,
                cancellationToken);

        if (solicitacao is null)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroNaoEncontrado("Solicitação não encontrada."));
        }

        var organizacao = await _dbContext
            .Organizacoes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == solicitacao.OrganizacaoId, cancellationToken);

        return await MapearDetalheAsync(
            solicitacao,
            organizacao?.Nome ?? "Organização",
            mascararDadosPessoais: false,
            cancellationToken);
    }

    public async Task<Result<RetiradaDetalhe>> CancelarAsync(
        Guid cidadaoId,
        Guid solicitacaoId,
        CancellationToken cancellationToken = default)
    {
        var solicitacao = await _dbContext
            .SolicitacoesDeRetirada
            .Include(item => item.Itens)
            .Include(item => item.Historico)
            .FirstOrDefaultAsync(
                item => item.Id == solicitacaoId && item.CidadaoId == cidadaoId,
                cancellationToken);

        if (solicitacao is null)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroNaoEncontrado("Solicitação não encontrada."));
        }

        if (!solicitacao.EstaAberta)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroDeConflito(
                    CodigosDeErro.EstadoInvalido,
                    "Somente solicitações abertas podem ser canceladas."));
        }

        var organizacao = await _dbContext
            .Organizacoes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == solicitacao.OrganizacaoId,
                cancellationToken);

        if (organizacao is null)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroNaoEncontrado("Organização não encontrada."));
        }

        var agora = _relogio.GetUtcNow();
        var fuso = FusoDaOrganizacao.Obter(organizacao);
        var periodos = await ObterPeriodosAsync(organizacao.Id, cancellationToken);
        var (horaInicio, horaFim) = periodos.TryGetValue(
            solicitacao.Periodo,
            out var configurado)
            ? configurado
            : (PoliticaDeRetirada.HoraInicioPadrao(solicitacao.Periodo),
                PoliticaDeRetirada.HoraFimPadrao(solicitacao.Periodo));

        _ = horaFim;

        if (!PoliticaDeRetirada.PodeCancelar(
                agora,
                solicitacao.DataLocal,
                horaInicio,
                fuso))
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroDeConflito(
                    CodigosDeErro.EstadoInvalido,
                    "O prazo para cancelamento encerrou. Fale com a organização."));
        }

        await TravarOrganizacaoAsync(organizacao.Id, cancellationToken);

        solicitacao.Cancelar(cidadaoId, agora);

        await AdicionarNotificacoesDaEquipeAsync(
            organizacao.Id,
            solicitacao.Id,
            TipoDeNotificacao.RetiradaCancelada,
            "Retirada cancelada pelo cidadão",
            $"A solicitação de {solicitacao.DataLocal:dd/MM/yyyy} foi cancelada pelo cidadão.",
            agora,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapearDetalheAsync(
            solicitacao,
            organizacao.Nome,
            mascararDadosPessoais: false,
            cancellationToken);
    }

    public async Task<PaginaDeRetiradas> ListarAsync(
        Guid organizacaoId,
        FiltroDeRetiradasDaOrganizacao filtro,
        CancellationToken cancellationToken = default)
    {
        var consulta = _dbContext
            .SolicitacoesDeRetirada
            .AsNoTracking()
            .Where(solicitacao => solicitacao.OrganizacaoId == organizacaoId);

        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            if (Enum.TryParse<EstadoSolicitacao>(
                    filtro.Estado,
                    ignoreCase: true,
                    out var estado))
            {
                consulta = consulta.Where(solicitacao => solicitacao.Estado == estado);
            }
            else
            {
                return new PaginaDeRetiradas([], filtro.Pagina, filtro.TamanhoPagina, 0);
            }
        }

        if (filtro.Data is DateOnly data)
        {
            consulta = consulta.Where(solicitacao => solicitacao.DataLocal == data);
        }

        if (filtro.MunicipioId is int municipioId)
        {
            consulta = consulta.Where(solicitacao => solicitacao.MunicipioId == municipioId);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var solicitacoes = await consulta
            .OrderBy(solicitacao => solicitacao.DataLocal)
            .ThenBy(solicitacao => solicitacao.CriadoEmUtc)
            .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
            .Take(filtro.TamanhoPagina)
            .ToListAsync(cancellationToken);

        var nomes = await ObterNomesDeOrganizacoesAsync(
            solicitacoes.Select(solicitacao => solicitacao.OrganizacaoId),
            cancellationToken);

        var itens = solicitacoes
            .Select(solicitacao => new RetiradaResumo(
                solicitacao.Id,
                solicitacao.Estado.ToString(),
                solicitacao.DataLocal,
                solicitacao.Periodo.ToString(),
                solicitacao.OrganizacaoId,
                nomes.GetValueOrDefault(solicitacao.OrganizacaoId, "Organização"),
                solicitacao.MunicipioId,
                solicitacao.MunicipioNome,
                solicitacao.Bairro,
                solicitacao.CriadoEmUtc))
            .ToList();

        return new PaginaDeRetiradas(
            itens,
            filtro.Pagina,
            filtro.TamanhoPagina,
            total);
    }

    public async Task<Result<RetiradaDetalhe>> ObterAsync(
        Guid organizacaoId,
        Guid solicitacaoId,
        CancellationToken cancellationToken = default)
    {
        var solicitacao = await CarregarDaOrganizacaoAsync(
            organizacaoId,
            solicitacaoId,
            cancellationToken);

        if (solicitacao is null)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroNaoEncontrado("Solicitação não encontrada."));
        }

        var organizacao = await _dbContext
            .Organizacoes
            .AsNoTracking()
            .FirstAsync(item => item.Id == organizacaoId, cancellationToken);

        return await MapearDetalheAsync(
            solicitacao,
            organizacao.Nome,
            mascararDadosPessoais: !solicitacao.PodeExibirDadosPessoais,
            cancellationToken);
    }

    public Task<Result<RetiradaDetalhe>> AgendarAsync(
        Guid organizacaoId,
        Guid atorUsuarioId,
        Guid solicitacaoId,
        AgendarRetiradaEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        return AlterarJanelaAsync(
            organizacaoId,
            atorUsuarioId,
            solicitacaoId,
            entrada.Data,
            entrada.Periodo,
            reagendar: false,
            cancellationToken);
    }

    public Task<Result<RetiradaDetalhe>> ReagendarAsync(
        Guid organizacaoId,
        Guid atorUsuarioId,
        Guid solicitacaoId,
        ReagendarRetiradaEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        return AlterarJanelaAsync(
            organizacaoId,
            atorUsuarioId,
            solicitacaoId,
            entrada.Data,
            entrada.Periodo,
            reagendar: true,
            cancellationToken);
    }

    public async Task<Result<RetiradaDetalhe>> RecusarAsync(
        Guid organizacaoId,
        Guid atorUsuarioId,
        Guid solicitacaoId,
        RecusarRetiradaEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        var solicitacao = await CarregarDaOrganizacaoAsync(
            organizacaoId,
            solicitacaoId,
            cancellationToken);

        if (solicitacao is null)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroNaoEncontrado("Solicitação não encontrada."));
        }

        if (solicitacao.Estado != EstadoSolicitacao.Solicitada)
        {
            return EstadoInvalido("Somente solicitações Solicitada podem ser recusadas.");
        }

        var agora = _relogio.GetUtcNow();
        solicitacao.Recusar(entrada.Motivo, atorUsuarioId, agora);
        await NotificarCidadaoAsync(
            solicitacao,
            TipoDeNotificacao.RetiradaRecusada,
            "Solicitação recusada",
            "Sua solicitação de retirada foi recusada pela organização.",
            agora,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var organizacao = await _dbContext
            .Organizacoes
            .AsNoTracking()
            .FirstAsync(item => item.Id == organizacaoId, cancellationToken);

        return await MapearDetalheAsync(
            solicitacao,
            organizacao.Nome,
            mascararDadosPessoais: true,
            cancellationToken);
    }

    public async Task<Result<RetiradaDetalhe>> ExecutarAsync(
        Guid organizacaoId,
        Guid atorUsuarioId,
        Guid solicitacaoId,
        ExecutarRetiradaEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        var solicitacao = await CarregarDaOrganizacaoAsync(
            organizacaoId,
            solicitacaoId,
            cancellationToken);

        if (solicitacao is null)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroNaoEncontrado("Solicitação não encontrada."));
        }

        if (solicitacao.Estado != EstadoSolicitacao.Agendada)
        {
            return EstadoInvalido("Somente solicitações Agendada podem ser executadas.");
        }

        var tipos = entrada.Itens.Select(item => item.TipoResiduoId).Distinct().ToList();
        var habilitados = await (
                from vinculo in _dbContext.OrganizacaoTipos.IgnoreQueryFilters()
                where vinculo.OrganizacaoId == organizacaoId
                    && vinculo.AceitaRetiradaDomiciliar
                    && tipos.Contains(vinculo.TipoResiduoId)
                select vinculo.TipoResiduoId)
            .ToListAsync(cancellationToken);

        if (habilitados.Count != tipos.Count)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroDeValidacao(
                    "itens",
                    "Todos os tipos devem aceitar retirada domiciliar nessa organização."));
        }

        var operadorNome = await ObterNomeDoUsuarioAsync(
            atorUsuarioId,
            cancellationToken);

        if (operadorNome is null)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroDeAcessoNegado("Operador não encontrado na organização."));
        }

        var agora = _relogio.GetUtcNow();
        var organizacao = await _dbContext
            .Organizacoes
            .AsNoTracking()
            .FirstAsync(item => item.Id == organizacaoId, cancellationToken);
        var fuso = FusoDaOrganizacao.Obter(organizacao);
        var dataLocal = FusoDaOrganizacao.DataLocal(fuso, agora);

        await TravarOrganizacaoAsync(organizacaoId, cancellationToken);

        solicitacao.MarcarColetada(atorUsuarioId, agora);

        foreach (var item in entrada.Itens)
        {
            _dbContext.RegistrosDeVolume.Add(
                RegistroDeVolume.CriarRetirada(
                    organizacaoId,
                    item.TipoResiduoId,
                    item.Quantidade,
                    solicitacao.Id,
                    atorUsuarioId,
                    operadorNome,
                    agora,
                    dataLocal));
        }

        await NotificarCidadaoAsync(
            solicitacao,
            TipoDeNotificacao.RetiradaColetada,
            "Retirada realizada",
            "Sua retirada foi registrada como realizada pela organização.",
            agora,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapearDetalheAsync(
            solicitacao,
            organizacao.Nome,
            mascararDadosPessoais: true,
            cancellationToken);
    }

    public async Task<Result<RetiradaDetalhe>> MarcarNaoRealizadaAsync(
        Guid organizacaoId,
        Guid atorUsuarioId,
        Guid solicitacaoId,
        NaoRealizadaEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        var solicitacao = await CarregarDaOrganizacaoAsync(
            organizacaoId,
            solicitacaoId,
            cancellationToken);

        if (solicitacao is null)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroNaoEncontrado("Solicitação não encontrada."));
        }

        if (solicitacao.Estado != EstadoSolicitacao.Agendada)
        {
            return EstadoInvalido("Somente solicitações Agendada podem ser encerradas.");
        }

        if (!Enum.TryParse<MotivoNaoRealizacao>(
                entrada.Motivo,
                ignoreCase: true,
                out var motivo))
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroDeValidacao("motivo", "Informe um motivo válido."));
        }

        var agora = _relogio.GetUtcNow();
        solicitacao.MarcarNaoRealizada(motivo, atorUsuarioId, agora);
        await NotificarCidadaoAsync(
            solicitacao,
            TipoDeNotificacao.RetiradaNaoRealizada,
            "Retirada não realizada",
            "A organização registrou que a retirada não pôde ser realizada.",
            agora,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var organizacao = await _dbContext
            .Organizacoes
            .AsNoTracking()
            .FirstAsync(item => item.Id == organizacaoId, cancellationToken);

        return await MapearDetalheAsync(
            solicitacao,
            organizacao.Nome,
            mascararDadosPessoais: true,
            cancellationToken);
    }

    private async Task<Result<RetiradaDetalhe>> AlterarJanelaAsync(
        Guid organizacaoId,
        Guid atorUsuarioId,
        Guid solicitacaoId,
        DateOnly? data,
        string? periodoTexto,
        bool reagendar,
        CancellationToken cancellationToken)
    {
        var solicitacao = await CarregarDaOrganizacaoAsync(
            organizacaoId,
            solicitacaoId,
            cancellationToken);

        if (solicitacao is null)
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroNaoEncontrado("Solicitação não encontrada."));
        }

        var estadoEsperado = reagendar
            ? EstadoSolicitacao.Agendada
            : EstadoSolicitacao.Solicitada;

        if (solicitacao.Estado != estadoEsperado)
        {
            return EstadoInvalido(
                reagendar
                    ? "Somente solicitações Agendada podem ser reagendadas."
                    : "Somente solicitações Solicitada podem ser agendadas.");
        }

        var organizacao = await _dbContext
            .Organizacoes
            .AsNoTracking()
            .FirstAsync(item => item.Id == organizacaoId, cancellationToken);

        var novaData = data ?? solicitacao.DataLocal;
        var periodo = solicitacao.Periodo;

        if (!string.IsNullOrWhiteSpace(periodoTexto)
            && !Enum.TryParse<PeriodoDeRetirada>(
                periodoTexto,
                ignoreCase: true,
                out periodo))
        {
            return Result.Fail<RetiradaDetalhe>(
                new ErroDeValidacao("periodo", "Informe um período válido."));
        }

        var mudouJanela = novaData != solicitacao.DataLocal || periodo != solicitacao.Periodo;

        if (mudouJanela)
        {
            var agora = _relogio.GetUtcNow();
            var fuso = FusoDaOrganizacao.Obter(organizacao);
            var hoje = FusoDaOrganizacao.Hoje(fuso, agora);

            if (!PoliticaDeRetirada.DentroDoHorizonte(hoje, novaData))
            {
                return Result.Fail<RetiradaDetalhe>(
                    new ErroDeValidacao(
                        "data",
                        $"A data deve estar entre amanhã e {PoliticaDeRetirada.DiasDeHorizonteMaximo} dias."));
            }

            await TravarOrganizacaoAsync(organizacaoId, cancellationToken);
            var conflito = await ValidarCapacidadeAsync(
                organizacaoId,
                novaData,
                periodo,
                ignorarSolicitacaoId: solicitacao.Id,
                cancellationToken);

            if (conflito is not null)
            {
                return Result.Fail<RetiradaDetalhe>(conflito);
            }
        }
        else if (!reagendar)
        {
            await TravarOrganizacaoAsync(organizacaoId, cancellationToken);
        }

        var agoraTransicao = _relogio.GetUtcNow();

        if (reagendar)
        {
            solicitacao.Reagendar(novaData, periodo, atorUsuarioId, agoraTransicao);
            await NotificarCidadaoAsync(
                solicitacao,
                TipoDeNotificacao.RetiradaReagendada,
                "Retirada reagendada",
                $"Sua retirada foi reagendada para {novaData:dd/MM/yyyy} ({periodo}).",
                agoraTransicao,
                cancellationToken);
        }
        else
        {
            solicitacao.Agendar(atorUsuarioId, agoraTransicao);
            await NotificarCidadaoAsync(
                solicitacao,
                TipoDeNotificacao.RetiradaAgendada,
                "Retirada agendada",
                $"Sua retirada foi confirmada para {novaData:dd/MM/yyyy} ({periodo}).",
                agoraTransicao,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapearDetalheAsync(
            solicitacao,
            organizacao.Nome,
            mascararDadosPessoais: false,
            cancellationToken);
    }

    private async Task<ErroDeConflito?> ValidarCapacidadeAsync(
        Guid organizacaoId,
        DateOnly data,
        PeriodoDeRetirada periodo,
        Guid ignorarSolicitacaoId,
        CancellationToken cancellationToken)
    {
        var bloqueada = await _dbContext
            .BloqueiosDeData
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                bloqueio => bloqueio.OrganizacaoId == organizacaoId
                    && bloqueio.DataLocal == data,
                cancellationToken);

        if (bloqueada)
        {
            return new ErroDeConflito(
                CodigosDeErro.DataBloqueada,
                "A data informada está bloqueada.");
        }

        var capacidade = await _dbContext
            .DisponibilidadesDeRetirada
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.OrganizacaoId == organizacaoId
                    && item.DiaDaSemana == data.DayOfWeek
                    && item.Periodo == periodo,
                cancellationToken);

        if (capacidade is null || capacidade.Capacidade <= 0)
        {
            return new ErroDeConflito(
                CodigosDeErro.JanelaIndisponivel,
                "Não há disponibilidade para a data e o período informados.");
        }

        var ocupacao = await _dbContext
            .SolicitacoesDeRetirada
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(
                solicitacao => solicitacao.OrganizacaoId == organizacaoId
                    && solicitacao.DataLocal == data
                    && solicitacao.Periodo == periodo
                    && solicitacao.Id != ignorarSolicitacaoId
                    && (solicitacao.Estado == EstadoSolicitacao.Solicitada
                        || solicitacao.Estado == EstadoSolicitacao.Agendada),
                cancellationToken);

        if (ocupacao >= capacidade.Capacidade)
        {
            return new ErroDeConflito(
                CodigosDeErro.CapacidadeEsgotada,
                "A janela escolhida não possui mais vagas.");
        }

        await GarantirJanelaAsync(
            organizacaoId,
            data,
            periodo,
            _relogio.GetUtcNow(),
            cancellationToken);

        return null;
    }

    private async Task GarantirJanelaAsync(
        Guid organizacaoId,
        DateOnly data,
        PeriodoDeRetirada periodo,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        var existe = await _dbContext
            .JanelasDeRetirada
            .IgnoreQueryFilters()
            .AnyAsync(
                janela => janela.OrganizacaoId == organizacaoId
                    && janela.DataLocal == data
                    && janela.Periodo == periodo,
                cancellationToken);

        if (!existe)
        {
            _dbContext.JanelasDeRetirada.Add(
                JanelaDeRetirada.Criar(organizacaoId, data, periodo, agora));
        }
    }

    private async Task<SolicitacaoDeRetirada?> CarregarDaOrganizacaoAsync(
        Guid organizacaoId,
        Guid solicitacaoId,
        CancellationToken cancellationToken)
    {
        return await _dbContext
            .SolicitacoesDeRetirada
            .Include(item => item.Itens)
            .Include(item => item.Historico)
            .FirstOrDefaultAsync(
                item => item.Id == solicitacaoId && item.OrganizacaoId == organizacaoId,
                cancellationToken);
    }

    private async Task<Organizacao?> ObterOrganizacaoPublicadaAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken)
    {
        return await _dbContext
            .Organizacoes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                organizacao => organizacao.Id == organizacaoId && organizacao.Publicada,
                cancellationToken);
    }

    private async Task<Dictionary<PeriodoDeRetirada, (TimeOnly Inicio, TimeOnly Fim)>>
        ObterPeriodosAsync(Guid organizacaoId, CancellationToken cancellationToken)
    {
        var periodos = await _dbContext
            .ConfiguracoesDePeriodo
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(periodo => periodo.OrganizacaoId == organizacaoId)
            .ToListAsync(cancellationToken);

        return periodos.ToDictionary(
            periodo => periodo.Periodo,
            periodo => (periodo.HoraInicio, periodo.HoraFim));
    }

    private async Task<string?> ObterNomeDoUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        return await _dbContext
            .Usuarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(usuario => usuario.Id == usuarioId)
            .Select(usuario => usuario.Nome)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, string>> ObterNomesDeOrganizacoesAsync(
        IEnumerable<Guid> organizacaoIds,
        CancellationToken cancellationToken)
    {
        var ids = organizacaoIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await _dbContext
            .Organizacoes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(organizacao => ids.Contains(organizacao.Id))
            .ToDictionaryAsync(
                organizacao => organizacao.Id,
                organizacao => organizacao.Nome,
                cancellationToken);
    }

    private async Task<Result<RetiradaDetalhe>> MapearDetalheAsync(
        SolicitacaoDeRetirada solicitacao,
        string organizacaoNome,
        bool mascararDadosPessoais,
        CancellationToken cancellationToken)
    {
        var tipos = await _dbContext
            .TiposResiduo
            .AsNoTracking()
            .Where(tipo => solicitacao.Itens
                .Select(item => item.TipoResiduoId)
                .Contains(tipo.Id))
            .ToDictionaryAsync(tipo => tipo.Id, cancellationToken);

        var volumes = await _dbContext
            .RegistrosDeVolume
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(volume => volume.OrganizacaoId == solicitacao.OrganizacaoId
                && volume.SolicitacaoId == solicitacao.Id)
            .ToListAsync(cancellationToken);

        var itens = solicitacao.Itens
            .OrderBy(item => item.TipoResiduoId)
            .Select(item =>
            {
                var tipo = tipos.GetValueOrDefault(item.TipoResiduoId);
                return new ItemDaRetiradaResposta(
                    item.TipoResiduoId,
                    tipo?.Codigo ?? string.Empty,
                    tipo?.Nome ?? "Tipo removido",
                    tipo?.Unidade.ToString() ?? string.Empty,
                    item.QuantidadeEstimada);
            })
            .ToList();

        var historico = solicitacao.Historico
            .OrderBy(item => item.OcorridoEmUtc)
            .Select(item => new HistoricoDaRetiradaResposta(
                item.EstadoAnterior?.ToString(),
                item.EstadoNovo.ToString(),
                item.AtorUsuarioId,
                item.OcorridoEmUtc,
                item.Motivo))
            .ToList();

        var volumesDaSolicitacao = volumes
            .GroupBy(volume => volume.TipoResiduoId)
            .Select(grupo =>
            {
                var tipo = tipos.GetValueOrDefault(grupo.Key);
                return new VolumeDaRetiradaResposta(
                    grupo.Key,
                    tipo?.Codigo ?? string.Empty,
                    tipo?.Nome ?? "Tipo removido",
                    tipo?.Unidade.ToString() ?? string.Empty,
                    grupo.Sum(volume => volume.Quantidade));
            })
            .OrderBy(item => item.Nome)
            .ToList();

        return Result.Ok(new RetiradaDetalhe(
            solicitacao.Id,
            solicitacao.Estado.ToString(),
            solicitacao.OrganizacaoId,
            organizacaoNome,
            mascararDadosPessoais ? null : solicitacao.NomeCidadao,
            mascararDadosPessoais ? null : solicitacao.TelefoneCidadao,
            mascararDadosPessoais,
            solicitacao.MunicipioNome,
            mascararDadosPessoais ? null : solicitacao.Bairro,
            mascararDadosPessoais ? null : solicitacao.Rua,
            mascararDadosPessoais ? null : solicitacao.Numero,
            mascararDadosPessoais ? null : solicitacao.Complemento,
            mascararDadosPessoais ? null : solicitacao.Cep,
            mascararDadosPessoais ? null : solicitacao.PontoReferencia,
            solicitacao.Observacoes,
            solicitacao.DataLocal,
            solicitacao.Periodo.ToString(),
            solicitacao.Motivo,
            solicitacao.CriadoEmUtc,
            solicitacao.AtualizadoEmUtc,
            itens,
            historico,
            volumesDaSolicitacao));
    }

    private Task NotificarCidadaoAsync(
        SolicitacaoDeRetirada solicitacao,
        TipoDeNotificacao tipo,
        string titulo,
        string mensagem,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _dbContext.Notificacoes.Add(
            Notificacao.Criar(
                solicitacao.CidadaoId,
                solicitacao.OrganizacaoId,
                tipo,
                solicitacao.Id,
                titulo,
                mensagem,
                agora));

        return Task.CompletedTask;
    }

    private async Task AdicionarNotificacoesDaEquipeAsync(
        Guid organizacaoId,
        Guid solicitacaoId,
        TipoDeNotificacao tipo,
        string titulo,
        string mensagem,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        var destinatarios = await _dbContext
            .Usuarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(usuario => usuario.OrganizacaoId == organizacaoId && usuario.Ativo)
            .Select(usuario => usuario.Id)
            .ToListAsync(cancellationToken);

        foreach (var destinatario in destinatarios)
        {
            _dbContext.Notificacoes.Add(
                Notificacao.Criar(
                    destinatario,
                    organizacaoId,
                    tipo,
                    solicitacaoId,
                    titulo,
                    mensagem,
                    agora));
        }
    }

    private async Task TravarOrganizacaoAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM [Organizacao] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {organizacaoId}",
            cancellationToken);
    }

    private static Result<RetiradaDetalhe> EstadoInvalido(string mensagem) =>
        Result.Fail<RetiradaDetalhe>(
            new ErroDeConflito(CodigosDeErro.EstadoInvalido, mensagem));
}
