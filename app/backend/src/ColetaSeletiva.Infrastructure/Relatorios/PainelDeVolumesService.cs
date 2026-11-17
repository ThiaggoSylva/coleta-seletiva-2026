using System.Globalization;
using System.Text;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Retiradas;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Retiradas;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.Infrastructure.Retiradas;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.Infrastructure.Relatorios;

internal sealed class PainelDeVolumesService : IPainelDeVolumes
{
    private const int DiasPadrao = 30;
    private const int DiasMaximos = 366;

    private readonly ColetaSeletivaDbContext _dbContext;
    private readonly TimeProvider _relogio;

    public PainelDeVolumesService(
        ColetaSeletivaDbContext dbContext,
        TimeProvider relogio)
    {
        _dbContext = dbContext;
        _relogio = relogio;
    }

    public async Task<ResumoDoPainel> ObterResumoAsync(
        Guid organizacaoId,
        FiltroDoPainel filtro,
        CancellationToken cancellationToken = default)
    {
        var (de, ate) = await NormalizarAsync(organizacaoId, filtro, cancellationToken);
        var volumes = await ConsultarAsync(organizacaoId, de, ate, filtro, cancellationToken);
        var tipos = await ObterTiposAsync(volumes, cancellationToken);

        var porTipo = volumes
            .GroupBy(volume => volume.TipoResiduoId)
            .Select(grupo =>
            {
                var tipo = tipos.GetValueOrDefault(grupo.Key);
                return new VolumePorTipoResposta(
                    grupo.Key,
                    tipo?.Codigo ?? string.Empty,
                    tipo?.Nome ?? "Tipo removido",
                    tipo?.Unidade.ToString() ?? string.Empty,
                    grupo.Sum(volume => volume.Quantidade));
            })
            .OrderBy(item => item.Nome)
            .ToList();

        var totaisPorUnidade = porTipo
            .GroupBy(item => item.Unidade)
            .Select(grupo => new TotalPorUnidadeResposta(
                grupo.Key,
                grupo.Sum(item => item.Quantidade)))
            .OrderBy(item => item.Unidade)
            .ToList();

        var retiradas = await _dbContext
            .SolicitacoesDeRetirada
            .AsNoTracking()
            .Where(solicitacao => solicitacao.OrganizacaoId == organizacaoId
                && solicitacao.DataLocal >= de
                && solicitacao.DataLocal <= ate)
            .GroupBy(solicitacao => solicitacao.Estado)
            .Select(grupo => new
            {
                Estado = grupo.Key,
                Total = grupo.Count(),
            })
            .ToListAsync(cancellationToken);

        var porOrigem = volumes
            .GroupBy(volume => new
            {
                volume.Origem,
                Unidade = tipos.GetValueOrDefault(volume.TipoResiduoId)?.Unidade.ToString()
                    ?? string.Empty,
            })
            .Select(grupo => new VolumePorOrigemResposta(
                grupo.Key.Origem.ToString(),
                grupo.Key.Unidade,
                grupo.Sum(volume => volume.Quantidade)))
            .OrderBy(item => item.Origem)
            .ThenBy(item => item.Unidade)
            .ToList();

        var nomesDePontos = await _dbContext
            .PontosDeColeta
            .AsNoTracking()
            .Where(ponto => ponto.OrganizacaoId == organizacaoId)
            .ToDictionaryAsync(ponto => ponto.Id, ponto => ponto.Nome, cancellationToken);

        var porPonto = volumes
            .Where(volume => volume.PontoId is not null)
            .GroupBy(volume => new
            {
                PontoId = volume.PontoId!.Value,
                Unidade = tipos.GetValueOrDefault(volume.TipoResiduoId)?.Unidade.ToString()
                    ?? string.Empty,
            })
            .Select(grupo => new VolumePorPontoResposta(
                grupo.Key.PontoId,
                nomesDePontos.GetValueOrDefault(grupo.Key.PontoId, "Ponto removido"),
                grupo.Key.Unidade,
                grupo.Sum(volume => volume.Quantidade)))
            .OrderBy(item => item.PontoNome)
            .ThenBy(item => item.Unidade)
            .ToList();

        return new ResumoDoPainel(
            de,
            ate,
            porTipo,
            totaisPorUnidade,
            retiradas
                .Select(item => new ContagemPorEstadoResposta(
                    item.Estado.ToString(),
                    item.Total))
                .OrderBy(item => item.Estado)
                .ToList(),
            porOrigem,
            porPonto);
    }

    public async Task<Result<SerieDoPainel>> ObterSerieAsync(
        Guid organizacaoId,
        FiltroDoPainel filtro,
        string agrupamento,
        CancellationToken cancellationToken = default)
    {
        var normalizado = agrupamento.Trim().ToLowerInvariant();
        if (normalizado is not ("dia" or "semana" or "mes"))
        {
            return Result.Fail<SerieDoPainel>(
                new ErroDeValidacao(
                    "agrupamento",
                    "O agrupamento deve ser dia, semana ou mês."));
        }

        var (de, ate) = await NormalizarAsync(organizacaoId, filtro, cancellationToken);
        var volumes = await ConsultarAsync(organizacaoId, de, ate, filtro, cancellationToken);
        var tipos = await ObterTiposAsync(volumes, cancellationToken);

        var pontos = volumes
            .GroupBy(volume => new
            {
                Inicio = InicioDoAgrupamento(volume.DataLocal, normalizado),
                volume.TipoResiduoId,
            })
            .Select(grupo =>
            {
                var tipo = tipos.GetValueOrDefault(grupo.Key.TipoResiduoId);
                return new
                {
                    grupo.Key.Inicio,
                    Item = new VolumePorTipoResposta(
                        grupo.Key.TipoResiduoId,
                        tipo?.Codigo ?? string.Empty,
                        tipo?.Nome ?? "Tipo removido",
                        tipo?.Unidade.ToString() ?? string.Empty,
                        grupo.Sum(volume => volume.Quantidade)),
                };
            })
            .GroupBy(item => item.Inicio)
            .OrderBy(grupo => grupo.Key)
            .Select(grupo => new PontoDaSerieResposta(
                grupo.Key,
                grupo
                    .Select(item => item.Item)
                    .OrderBy(item => item.Nome)
                    .ToList()))
            .ToList();

        return Result.Ok(new SerieDoPainel(normalizado, de, ate, pontos));
    }

    public async Task<ArquivoCsvResposta> ExportarCsvAsync(
        Guid organizacaoId,
        FiltroDoPainel filtro,
        CancellationToken cancellationToken = default)
    {
        var (de, ate) = await NormalizarAsync(organizacaoId, filtro, cancellationToken);
        var volumes = await ConsultarAsync(organizacaoId, de, ate, filtro, cancellationToken);
        var tipos = await ObterTiposAsync(volumes, cancellationToken);

        var nomesDePontos = await _dbContext
            .PontosDeColeta
            .AsNoTracking()
            .Where(ponto => ponto.OrganizacaoId == organizacaoId)
            .ToDictionaryAsync(ponto => ponto.Id, ponto => ponto.Nome, cancellationToken);

        var construtor = new StringBuilder();
        construtor.AppendLine(
            "Data;Tipo;Unidade;Quantidade;Sinal;Origem;Operador;Ponto;Solicitacao;RegistroOriginal;Motivo");

        foreach (var volume in volumes
                     .OrderBy(volume => volume.DataLocal)
                     .ThenBy(volume => volume.OcorridoEmUtc)
                     .ThenBy(volume => volume.Id))
        {
            var tipo = tipos.GetValueOrDefault(volume.TipoResiduoId);
            var sinal = volume.Origem != OrigemDoVolume.Ajuste
                ? "="
                : volume.Quantidade < 0 ? "-" : "+";

            construtor
                .Append(Escapar(volume.DataLocal.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)))
                .Append(';')
                .Append(Escapar(tipo?.Nome ?? "Tipo removido"))
                .Append(';')
                .Append(Escapar(tipo?.Unidade.ToString() ?? string.Empty))
                .Append(';')
                .Append(Math.Abs(volume.Quantidade).ToString("0.000", CultureInfo.InvariantCulture).Replace('.', ','))
                .Append(';')
                .Append(sinal)
                .Append(';')
                .Append(Escapar(volume.Origem.ToString()))
                .Append(';')
                .Append(Escapar(volume.OperadorNome))
                .Append(';')
                .Append(Escapar(volume.PontoId is Guid pontoId
                    ? nomesDePontos.GetValueOrDefault(pontoId, "Ponto removido")
                    : string.Empty))
                .Append(';')
                .Append(volume.SolicitacaoId?.ToString() ?? string.Empty)
                .Append(';')
                .Append(volume.RegistroOriginalId?.ToString() ?? string.Empty)
                .Append(';')
                .Append(Escapar(volume.Motivo ?? string.Empty))
                .AppendLine();
        }

        var conteudo = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(construtor.ToString()))
            .ToArray();

        return new ArquivoCsvResposta(
            $"volumes-{de:yyyyMMdd}-{ate:yyyyMMdd}.csv",
            conteudo);
    }

    private async Task<IReadOnlyList<RegistroDeVolume>> ConsultarAsync(
        Guid organizacaoId,
        DateOnly de,
        DateOnly ate,
        FiltroDoPainel filtro,
        CancellationToken cancellationToken)
    {
        var consulta = _dbContext
            .RegistrosDeVolume
            .AsNoTracking()
            .Where(volume => volume.OrganizacaoId == organizacaoId
                && volume.DataLocal >= de
                && volume.DataLocal <= ate);

        if (filtro.TipoResiduoId is Guid tipoResiduoId)
        {
            consulta = consulta.Where(volume => volume.TipoResiduoId == tipoResiduoId);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Origem)
            && Enum.TryParse<OrigemDoVolume>(filtro.Origem, true, out var origem))
        {
            consulta = consulta.Where(volume => volume.Origem == origem);
        }

        return await consulta.ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, TipoResiduo>> ObterTiposAsync(
        IReadOnlyCollection<RegistroDeVolume> volumes,
        CancellationToken cancellationToken)
    {
        var ids = volumes.Select(volume => volume.TipoResiduoId).Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await _dbContext
            .TiposResiduo
            .AsNoTracking()
            .Where(tipo => ids.Contains(tipo.Id))
            .ToDictionaryAsync(tipo => tipo.Id, cancellationToken);
    }

    private async Task<(DateOnly De, DateOnly Ate)> NormalizarAsync(
        Guid organizacaoId,
        FiltroDoPainel filtro,
        CancellationToken cancellationToken)
    {
        var organizacao = await _dbContext
            .Organizacoes
            .AsNoTracking()
            .FirstAsync(item => item.Id == organizacaoId, cancellationToken);
        var fuso = FusoDaOrganizacao.Obter(organizacao);
        var hoje = FusoDaOrganizacao.Hoje(fuso, _relogio.GetUtcNow());

        var ate = filtro.Ate ?? hoje;
        var de = filtro.De ?? ate.AddDays(-DiasPadrao);

        if (de > ate)
        {
            (de, ate) = (ate, de);
        }

        if (ate.DayNumber - de.DayNumber > DiasMaximos)
        {
            de = ate.AddDays(-DiasMaximos);
        }

        return (de, ate);
    }

    private static DateOnly InicioDoAgrupamento(DateOnly data, string agrupamento)
    {
        return agrupamento switch
        {
            "semana" => data.AddDays(-((int)data.DayOfWeek == 0 ? 6 : (int)data.DayOfWeek - 1)),
            "mes" => new DateOnly(data.Year, data.Month, 1),
            _ => data,
        };
    }

    private static string Escapar(string valor)
    {
        var normalizado = valor ?? string.Empty;

        if (normalizado.Length > 0
            && normalizado[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            normalizado = "'" + normalizado;
        }

        if (normalizado.Contains(';', StringComparison.Ordinal)
            || normalizado.Contains('"', StringComparison.Ordinal)
            || normalizado.Contains('\n', StringComparison.Ordinal)
            || normalizado.Contains('\r', StringComparison.Ordinal))
        {
            normalizado = "\"" + normalizado.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return normalizado;
    }
}
