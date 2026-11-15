using System.Globalization;
using ColetaSeletiva.Application.Catalogo;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.PerfilDaOrganizacao;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure.Persistencia;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.Infrastructure.Perfis;

internal sealed class PerfilDaOrganizacaoService : IPerfilDaOrganizacao
{
    private readonly ColetaSeletivaDbContext _dbContext;
    private readonly TimeProvider _relogio;

    public PerfilDaOrganizacaoService(
        ColetaSeletivaDbContext dbContext,
        TimeProvider relogio)
    {
        _dbContext = dbContext;
        _relogio = relogio;
    }

    public async Task<PerfilDaOrganizacaoResposta?> ObterAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken = default)
    {
        var organizacao = await _dbContext
            .Organizacoes
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == organizacaoId, cancellationToken);

        if (organizacao is null)
        {
            return null;
        }

        var municipios = await (
                from vinculo in _dbContext.OrganizacaoMunicipios.AsNoTracking()
                join municipio in _dbContext.Municipios.AsNoTracking()
                    on vinculo.MunicipioId equals municipio.Id
                where vinculo.OrganizacaoId == organizacaoId
                orderby municipio.Nome, municipio.Id
                select new MunicipioDoCatalogo(municipio.Id, municipio.Nome, municipio.Uf))
            .ToListAsync(cancellationToken);

        var periodos = await _dbContext
            .ConfiguracoesDePeriodo
            .AsNoTracking()
            .Where(periodo => periodo.OrganizacaoId == organizacaoId)
            .OrderBy(periodo => periodo.HoraInicio)
            .ToListAsync(cancellationToken);

        var tipos = await (
                from vinculo in _dbContext.OrganizacaoTipos.AsNoTracking()
                join tipo in _dbContext.TiposResiduo.AsNoTracking()
                    on vinculo.TipoResiduoId equals tipo.Id
                where vinculo.OrganizacaoId == organizacaoId
                orderby tipo.Nome
                select new
                {
                    Tipo = tipo,
                    vinculo.AceitaRetiradaDomiciliar,
                })
            .ToListAsync(cancellationToken);

        return new PerfilDaOrganizacaoResposta(
            Id: organizacao.Id,
            Nome: organizacao.Nome,
            Tipo: organizacao.Tipo.ToString(),
            ContatoEmail: organizacao.ContatoEmail,
            ContatoTelefone: organizacao.ContatoTelefone,
            Fuso: organizacao.Fuso,
            LimiteSolicitacoesAbertas: organizacao.LimiteSolicitacoesAbertas,
            Publicada: organizacao.Publicada,
            Municipios: municipios,
            Periodos: periodos
                .Select(periodo => new PeriodoDaOrganizacaoResposta(
                    Periodo: periodo.Periodo.ToString(),
                    HoraInicio: periodo.HoraInicio.ToString(
                        LimitesDoPerfilDaOrganizacao.FormatoDeHora,
                        CultureInfo.InvariantCulture),
                    HoraFim: periodo.HoraFim.ToString(
                        LimitesDoPerfilDaOrganizacao.FormatoDeHora,
                        CultureInfo.InvariantCulture)))
                .ToList(),
            Tipos: tipos
                .Select(item => new TipoHabilitadoResposta(
                    TipoResiduoId: item.Tipo.Id,
                    Codigo: item.Tipo.Codigo,
                    Nome: item.Tipo.Nome,
                    Categoria: item.Tipo.Categoria.ToString(),
                    Unidade: item.Tipo.Unidade.ToString(),
                    AceitaRetiradaDomiciliar: item.AceitaRetiradaDomiciliar))
                .ToList());
    }

    public async Task<Result<PerfilDaOrganizacaoResposta>> AtualizarAsync(
        Guid organizacaoId,
        AtualizarPerfilDaOrganizacaoEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        var organizacao = await _dbContext
            .Organizacoes
            .FirstOrDefaultAsync(item => item.Id == organizacaoId, cancellationToken);

        if (organizacao is null)
        {
            return Result.Fail<PerfilDaOrganizacaoResposta>(
                new ErroNaoEncontrado("Organização não encontrada."));
        }

        organizacao.AtualizarPerfil(
            entrada.Nome,
            entrada.Tipo,
            entrada.ContatoEmail,
            entrada.ContatoTelefone,
            entrada.Fuso,
            entrada.LimiteSolicitacoesAbertas,
            _relogio.GetUtcNow());

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ObterComoResultadoAsync(organizacaoId, cancellationToken);
    }

    public async Task<Result<PerfilDaOrganizacaoResposta>> DefinirMunicipiosAsync(
        Guid organizacaoId,
        IReadOnlyCollection<int> municipioIds,
        CancellationToken cancellationToken = default)
    {
        var solicitados = municipioIds.Distinct().ToList();

        var existentes = await _dbContext
            .Municipios
            .AsNoTracking()
            .Where(municipio => solicitados.Contains(municipio.Id))
            .Select(municipio => municipio.Id)
            .ToListAsync(cancellationToken);

        var inexistentes = solicitados.Except(existentes).ToList();
        if (inexistentes.Count > 0)
        {
            return Result.Fail<PerfilDaOrganizacaoResposta>(
                new ErroDeValidacao("municipioIds", "Um ou mais municípios não existem no catálogo."));
        }

        var atuais = await _dbContext
            .OrganizacaoMunicipios
            .Where(vinculo => vinculo.OrganizacaoId == organizacaoId)
            .ToListAsync(cancellationToken);

        var remover = atuais
            .Where(vinculo => !solicitados.Contains(vinculo.MunicipioId))
            .ToList();

        if (remover.Count > 0)
        {
            var removidos = remover.Select(vinculo => vinculo.MunicipioId).ToList();
            var emUso = await _dbContext
                .PontosDeColeta
                .AsNoTracking()
                .AnyAsync(
                    ponto => removidos.Contains(ponto.MunicipioId),
                    cancellationToken);

            if (emUso)
            {
                return Result.Fail<PerfilDaOrganizacaoResposta>(
                    new ErroDeConflito(
                        CodigosDeErro.VinculoEmUso,
                        "Um dos municípios removidos é usado por ponto de coleta. "
                        + "Ajuste os pontos antes de remover o município."));
            }

            _dbContext.OrganizacaoMunicipios.RemoveRange(remover);
        }

        var manter = atuais.Select(vinculo => vinculo.MunicipioId).ToHashSet();
        var agora = _relogio.GetUtcNow();

        foreach (var municipioId in solicitados.Where(id => !manter.Contains(id)))
        {
            _dbContext.OrganizacaoMunicipios.Add(
                OrganizacaoMunicipio.Criar(municipioId, agora));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ObterComoResultadoAsync(organizacaoId, cancellationToken);
    }

    public async Task<Result<PerfilDaOrganizacaoResposta>> DefinirPeriodosAsync(
        Guid organizacaoId,
        IReadOnlyCollection<PeriodoDaOrganizacaoEntrada> periodos,
        CancellationToken cancellationToken = default)
    {
        var atuais = await _dbContext
            .ConfiguracoesDePeriodo
            .Where(periodo => periodo.OrganizacaoId == organizacaoId)
            .ToListAsync(cancellationToken);

        _dbContext.ConfiguracoesDePeriodo.RemoveRange(atuais);

        var agora = _relogio.GetUtcNow();

        foreach (var periodo in periodos)
        {
            _ = Enum.TryParse<PeriodoDeRetirada>(
                periodo.Periodo,
                ignoreCase: true,
                out var periodoConvertido);

            var inicio = TimeOnly.ParseExact(
                periodo.HoraInicio,
                LimitesDoPerfilDaOrganizacao.FormatoDeHora,
                CultureInfo.InvariantCulture);
            var fim = TimeOnly.ParseExact(
                periodo.HoraFim,
                LimitesDoPerfilDaOrganizacao.FormatoDeHora,
                CultureInfo.InvariantCulture);

            _dbContext.ConfiguracoesDePeriodo.Add(
                ConfiguracaoDePeriodo.Criar(periodoConvertido, inicio, fim, agora));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ObterComoResultadoAsync(organizacaoId, cancellationToken);
    }

    public async Task<Result<PerfilDaOrganizacaoResposta>> DefinirTiposAsync(
        Guid organizacaoId,
        IReadOnlyCollection<TipoHabilitadoEntrada> tipos,
        CancellationToken cancellationToken = default)
    {
        var solicitados = tipos
            .GroupBy(tipo => tipo.TipoResiduoId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First().AceitaRetiradaDomiciliar);

        var existentes = await _dbContext
            .TiposResiduo
            .AsNoTracking()
            .Where(tipo => solicitados.Keys.Contains(tipo.Id))
            .Select(tipo => tipo.Id)
            .ToListAsync(cancellationToken);

        if (existentes.Count != solicitados.Count)
        {
            return Result.Fail<PerfilDaOrganizacaoResposta>(
                new ErroDeValidacao("tipos", "Um ou mais tipos não existem no catálogo."));
        }

        var atuais = await _dbContext
            .OrganizacaoTipos
            .Where(vinculo => vinculo.OrganizacaoId == organizacaoId)
            .ToListAsync(cancellationToken);

        var remover = atuais
            .Where(vinculo => !solicitados.ContainsKey(vinculo.TipoResiduoId))
            .ToList();

        if (remover.Count > 0)
        {
            var removidos = remover.Select(vinculo => vinculo.TipoResiduoId).ToList();
            var emUso = await _dbContext
                .PontoTipos
                .AsNoTracking()
                .AnyAsync(tipo => removidos.Contains(tipo.TipoResiduoId), cancellationToken);

            if (emUso)
            {
                return Result.Fail<PerfilDaOrganizacaoResposta>(
                    new ErroDeConflito(
                        CodigosDeErro.VinculoEmUso,
                        "Um dos tipos removidos é usado por ponto de coleta. "
                        + "Ajuste os pontos antes de desabilitar o tipo."));
            }

            _dbContext.OrganizacaoTipos.RemoveRange(remover);
        }

        var agora = _relogio.GetUtcNow();

        foreach (var atual in atuais.Where(vinculo => solicitados.ContainsKey(vinculo.TipoResiduoId)))
        {
            var aceita = solicitados[atual.TipoResiduoId];
            if (atual.AceitaRetiradaDomiciliar != aceita)
            {
                atual.Atualizar(aceita, agora);
            }
        }

        var existentesNaOrganizacao = atuais
            .Select(vinculo => vinculo.TipoResiduoId)
            .ToHashSet();

        foreach (var (tipoResiduoId, aceita) in solicitados)
        {
            if (!existentesNaOrganizacao.Contains(tipoResiduoId))
            {
                _dbContext.OrganizacaoTipos.Add(
                    OrganizacaoTipo.Criar(tipoResiduoId, aceita, agora));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ObterComoResultadoAsync(organizacaoId, cancellationToken);
    }

    public async Task<Result<PublicacaoDaOrganizacaoResposta>> DefinirPublicacaoAsync(
        Guid organizacaoId,
        bool publicada,
        CancellationToken cancellationToken = default)
    {
        var organizacao = await _dbContext
            .Organizacoes
            .FirstOrDefaultAsync(item => item.Id == organizacaoId, cancellationToken);

        if (organizacao is null)
        {
            return Result.Fail<PublicacaoDaOrganizacaoResposta>(
                new ErroNaoEncontrado("Organização não encontrada."));
        }

        if (publicada)
        {
            var temMunicipio = await _dbContext
                .OrganizacaoMunicipios
                .AsNoTracking()
                .AnyAsync(vinculo => vinculo.OrganizacaoId == organizacaoId, cancellationToken);

            if (!temMunicipio)
            {
                return Result.Fail<PublicacaoDaOrganizacaoResposta>(
                    new ErroDeConflito(
                        CodigosDeErro.PublicacaoIncompleta,
                        "Informe ao menos um município atendido antes de publicar."));
            }

            var temTipo = await _dbContext
                .OrganizacaoTipos
                .AsNoTracking()
                .AnyAsync(vinculo => vinculo.OrganizacaoId == organizacaoId, cancellationToken);

            if (!temTipo)
            {
                return Result.Fail<PublicacaoDaOrganizacaoResposta>(
                    new ErroDeConflito(
                        CodigosDeErro.PublicacaoIncompleta,
                        "Habilite ao menos um tipo de resíduo antes de publicar."));
            }
        }

        organizacao.DefinirPublicacao(publicada, _relogio.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(new PublicacaoDaOrganizacaoResposta(
            organizacao.Publicada,
            organizacao.AtualizadoEmUtc));
    }

    private async Task<Result<PerfilDaOrganizacaoResposta>> ObterComoResultadoAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken)
    {
        var perfil = await ObterAsync(organizacaoId, cancellationToken);

        return perfil is null
            ? Result.Fail<PerfilDaOrganizacaoResposta>(
                new ErroNaoEncontrado("Organização não encontrada."))
            : Result.Ok(perfil);
    }
}
