using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Retiradas;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure.Persistencia;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.Infrastructure.Retiradas;

internal sealed class DisponibilidadeDeRetiradaService : IDisponibilidadeDeRetirada
{
    private readonly ColetaSeletivaDbContext _dbContext;
    private readonly TimeProvider _relogio;

    public DisponibilidadeDeRetiradaService(
        ColetaSeletivaDbContext dbContext,
        TimeProvider relogio)
    {
        _dbContext = dbContext;
        _relogio = relogio;
    }

    public async Task<IReadOnlyList<DisponibilidadeDiaResposta>> ObterAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken = default)
    {
        var disponibilidades = await _dbContext
            .DisponibilidadesDeRetirada
            .AsNoTracking()
            .Where(disponibilidade => disponibilidade.OrganizacaoId == organizacaoId)
            .OrderBy(disponibilidade => disponibilidade.DiaDaSemana)
            .ThenBy(disponibilidade => disponibilidade.Periodo)
            .ToListAsync(cancellationToken);

        return disponibilidades
            .Select(disponibilidade => new DisponibilidadeDiaResposta(
                disponibilidade.DiaDaSemana.ToString(),
                disponibilidade.Periodo.ToString(),
                disponibilidade.Capacidade))
            .ToList();
    }

    public async Task<Result<IReadOnlyList<DisponibilidadeDiaResposta>>> DefinirAsync(
        Guid organizacaoId,
        IReadOnlyCollection<DisponibilidadeDiaEntrada> dias,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dias);

        var solicitados = new List<(DayOfWeek Dia, PeriodoDeRetirada Periodo, int Capacidade)>();

        foreach (var dia in dias)
        {
            if (!Enum.TryParse<DayOfWeek>(dia.DiaDaSemana, ignoreCase: true, out var diaSemana)
                || !Enum.IsDefined(diaSemana))
            {
                return Result.Fail<IReadOnlyList<DisponibilidadeDiaResposta>>(
                    new ErroDeValidacao("diaDaSemana", "Informe um dia da semana válido."));
            }

            if (!Enum.TryParse<PeriodoDeRetirada>(
                    dia.Periodo,
                    ignoreCase: true,
                    out var periodo)
                || !Enum.IsDefined(periodo))
            {
                return Result.Fail<IReadOnlyList<DisponibilidadeDiaResposta>>(
                    new ErroDeValidacao("periodo", "Informe um período válido."));
            }

            if (dia.Capacidade is < 0 or > DisponibilidadeDeRetirada.CapacidadeMaxima)
            {
                return Result.Fail<IReadOnlyList<DisponibilidadeDiaResposta>>(
                    new ErroDeValidacao(
                        "capacidade",
                        $"A capacidade deve estar entre 0 e {DisponibilidadeDeRetirada.CapacidadeMaxima}."));
            }

            solicitados.Add((diaSemana, periodo, dia.Capacidade));
        }

        var chaves = solicitados
            .Select(item => (item.Dia, item.Periodo))
            .Distinct()
            .ToList();

        if (chaves.Count != solicitados.Count)
        {
            return Result.Fail<IReadOnlyList<DisponibilidadeDiaResposta>>(
                new ErroDeValidacao(
                    "dias",
                    "Não repita a combinação de dia da semana e período."));
        }

        var agora = _relogio.GetUtcNow();
        var existentes = await _dbContext
            .DisponibilidadesDeRetirada
            .Where(disponibilidade => disponibilidade.OrganizacaoId == organizacaoId)
            .ToListAsync(cancellationToken);

        var porChave = existentes.ToDictionary(
            disponibilidade => (disponibilidade.DiaDaSemana, disponibilidade.Periodo));

        foreach (var item in solicitados)
        {
            if (porChave.TryGetValue((item.Dia, item.Periodo), out var existente))
            {
                existente.DefinirCapacidade(item.Capacidade, agora);
            }
            else
            {
                _dbContext.DisponibilidadesDeRetirada.Add(
                    DisponibilidadeDeRetirada.Criar(
                        item.Dia,
                        item.Periodo,
                        item.Capacidade,
                        agora));
            }
        }

        var remover = existentes
            .Where(disponibilidade => !solicitados.Any(item =>
                item.Dia == disponibilidade.DiaDaSemana
                && item.Periodo == disponibilidade.Periodo))
            .ToList();

        if (remover.Count > 0)
        {
            _dbContext.DisponibilidadesDeRetirada.RemoveRange(remover);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(await ObterAsync(organizacaoId, cancellationToken));
    }

    public async Task<IReadOnlyList<BloqueioDeDataResposta>> ListarBloqueiosAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken = default)
    {
        var bloqueios = await _dbContext
            .BloqueiosDeData
            .AsNoTracking()
            .Where(bloqueio => bloqueio.OrganizacaoId == organizacaoId)
            .OrderBy(bloqueio => bloqueio.DataLocal)
            .ToListAsync(cancellationToken);

        return bloqueios
            .Select(bloqueio => new BloqueioDeDataResposta(
                bloqueio.Id,
                bloqueio.DataLocal,
                bloqueio.Motivo,
                bloqueio.CriadoPorUsuarioId,
                bloqueio.CriadoEmUtc))
            .ToList();
    }

    public async Task<Result<BloqueioDeDataResposta>> CriarBloqueioAsync(
        Guid organizacaoId,
        Guid usuarioId,
        BloqueioDeDataEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        var existente = await _dbContext
            .BloqueiosDeData
            .AsNoTracking()
            .AnyAsync(
                bloqueio => bloqueio.OrganizacaoId == organizacaoId
                    && bloqueio.DataLocal == entrada.Data,
                cancellationToken);

        if (existente)
        {
            return Result.Fail<BloqueioDeDataResposta>(
                new ErroDeConflito(
                    CodigosDeErro.DataBloqueada,
                    "A data já está bloqueada."));
        }

        var bloqueio = BloqueioDeData.Criar(
            entrada.Data,
            entrada.Motivo,
            usuarioId,
            _relogio.GetUtcNow());

        _dbContext.BloqueiosDeData.Add(bloqueio);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(new BloqueioDeDataResposta(
            bloqueio.Id,
            bloqueio.DataLocal,
            bloqueio.Motivo,
            bloqueio.CriadoPorUsuarioId,
            bloqueio.CriadoEmUtc));
    }

    public async Task<Result> RemoverBloqueioAsync(
        Guid organizacaoId,
        Guid bloqueioId,
        CancellationToken cancellationToken = default)
    {
        var bloqueio = await _dbContext
            .BloqueiosDeData
            .FirstOrDefaultAsync(
                item => item.Id == bloqueioId && item.OrganizacaoId == organizacaoId,
                cancellationToken);

        if (bloqueio is null)
        {
            return Result.Fail(new ErroNaoEncontrado("Bloqueio não encontrado."));
        }

        _dbContext.BloqueiosDeData.Remove(bloqueio);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
