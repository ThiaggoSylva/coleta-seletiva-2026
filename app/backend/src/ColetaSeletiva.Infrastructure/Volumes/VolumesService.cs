using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Retiradas;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Retiradas;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.Infrastructure.Retiradas;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.Infrastructure.Volumes;

internal sealed class VolumesService : IVolumes
{
    private readonly ColetaSeletivaDbContext _dbContext;
    private readonly TimeProvider _relogio;

    public VolumesService(ColetaSeletivaDbContext dbContext, TimeProvider relogio)
    {
        _dbContext = dbContext;
        _relogio = relogio;
    }

    public async Task<PaginaDeVolumes> ListarAsync(
        Guid organizacaoId,
        FiltroDeVolumes filtro,
        CancellationToken cancellationToken = default)
    {
        var consulta = _dbContext
            .RegistrosDeVolume
            .AsNoTracking()
            .Where(volume => volume.OrganizacaoId == organizacaoId);

        if (filtro.De is DateOnly de)
        {
            consulta = consulta.Where(volume => volume.DataLocal >= de);
        }

        if (filtro.Ate is DateOnly ate)
        {
            consulta = consulta.Where(volume => volume.DataLocal <= ate);
        }

        if (filtro.TipoResiduoId is Guid tipoResiduoId)
        {
            consulta = consulta.Where(volume => volume.TipoResiduoId == tipoResiduoId);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Origem))
        {
            if (!Enum.TryParse<OrigemDoVolume>(filtro.Origem, true, out var origem))
            {
                return new PaginaDeVolumes(
                    [],
                    filtro.Pagina,
                    filtro.TamanhoPagina,
                    0);
            }

            consulta = consulta.Where(volume => volume.Origem == origem);
        }

        if (filtro.PontoId is Guid pontoId)
        {
            consulta = consulta.Where(volume => volume.PontoId == pontoId);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var volumes = await consulta
            .OrderByDescending(volume => volume.DataLocal)
            .ThenByDescending(volume => volume.OcorridoEmUtc)
            .ThenByDescending(volume => volume.Id)
            .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
            .Take(filtro.TamanhoPagina)
            .ToListAsync(cancellationToken);

        var itens = await MapearVariosAsync(volumes, cancellationToken);

        return new PaginaDeVolumes(
            itens,
            filtro.Pagina,
            filtro.TamanhoPagina,
            total);
    }

    public async Task<Result<VolumeResposta>> RegistrarRecebimentoAsync(
        Guid organizacaoId,
        Guid operadorUsuarioId,
        RegistrarRecebimentoEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        var ponto = await _dbContext
            .PontosDeColeta
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == entrada.PontoId
                    && item.OrganizacaoId == organizacaoId
                    && item.ExcluidoEmUtc == null,
                cancellationToken);

        if (ponto is null)
        {
            return Result.Fail<VolumeResposta>(
                new ErroNaoEncontrado("Ponto de coleta não encontrado."));
        }

        if (ponto.Status != StatusPonto.Ativo)
        {
            return Result.Fail<VolumeResposta>(
                new ErroDeConflito(
                    CodigosDeErro.EstadoInvalido,
                    "Somente pontos ativos podem registrar recebimentos."));
        }

        var aceitoPeloPonto = await _dbContext
            .PontoTipos
            .AsNoTracking()
            .AnyAsync(
                item => item.OrganizacaoId == organizacaoId
                    && item.PontoColetaId == entrada.PontoId
                    && item.TipoResiduoId == entrada.TipoResiduoId,
                cancellationToken);

        if (!aceitoPeloPonto)
        {
            return Result.Fail<VolumeResposta>(
                new ErroDeValidacao(
                    "tipoResiduoId",
                    "O ponto não aceita o tipo de resíduo informado."));
        }

        var habilitado = await _dbContext
            .OrganizacaoTipos
            .AsNoTracking()
            .AnyAsync(
                item => item.OrganizacaoId == organizacaoId
                    && item.TipoResiduoId == entrada.TipoResiduoId,
                cancellationToken);

        if (!habilitado)
        {
            return Result.Fail<VolumeResposta>(
                new ErroDeValidacao(
                    "tipoResiduoId",
                    "O tipo de resíduo não está habilitado na organização."));
        }

        var operadorNome = await ObterNomeDoOperadorAsync(
            organizacaoId,
            operadorUsuarioId,
            cancellationToken);

        if (operadorNome is null)
        {
            return Result.Fail<VolumeResposta>(
                new ErroDeAcessoNegado("Operador não encontrado na organização."));
        }

        var organizacao = await _dbContext
            .Organizacoes
            .AsNoTracking()
            .FirstAsync(item => item.Id == organizacaoId, cancellationToken);
        var agora = _relogio.GetUtcNow();
        var ocorridoEm = entrada.OcorridoEmUtc ?? agora;

        if (ocorridoEm > agora.AddMinutes(5))
        {
            return Result.Fail<VolumeResposta>(
                new ErroDeValidacao(
                    "ocorridoEmUtc",
                    "A data do recebimento não pode estar no futuro."));
        }

        var fuso = FusoDaOrganizacao.Obter(organizacao);
        var dataLocal = FusoDaOrganizacao.DataLocal(fuso, ocorridoEm);

        var volume = RegistroDeVolume.CriarRecebimento(
            organizacaoId,
            entrada.TipoResiduoId,
            entrada.Quantidade,
            entrada.PontoId,
            operadorUsuarioId,
            operadorNome,
            ocorridoEm,
            dataLocal);

        _dbContext.RegistrosDeVolume.Add(volume);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(await MapearAsync(volume, cancellationToken));
    }

    public async Task<Result<VolumeResposta>> AjustarAsync(
        Guid organizacaoId,
        Guid operadorUsuarioId,
        Guid volumeId,
        AjustarVolumeEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        var original = await _dbContext
            .RegistrosDeVolume
            .FirstOrDefaultAsync(
                volume => volume.Id == volumeId && volume.OrganizacaoId == organizacaoId,
                cancellationToken);

        if (original is null)
        {
            return Result.Fail<VolumeResposta>(
                new ErroNaoEncontrado("Registro de volume não encontrado."));
        }

        if (original.Origem == OrigemDoVolume.Ajuste || original.RegistroOriginalId is not null)
        {
            return Result.Fail<VolumeResposta>(
                new ErroDeConflito(
                    CodigosDeErro.RegistroImutavel,
                    "Ajustes devem referenciar o registro original, não outro ajuste."));
        }

        var operadorNome = await ObterNomeDoOperadorAsync(
            organizacaoId,
            operadorUsuarioId,
            cancellationToken);

        if (operadorNome is null)
        {
            return Result.Fail<VolumeResposta>(
                new ErroDeAcessoNegado("Operador não encontrado na organização."));
        }

        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM [RegistroVolume] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {volumeId}",
            cancellationToken);

        var ajustesExistentes = await _dbContext
            .RegistrosDeVolume
            .AsNoTracking()
            .Where(volume => volume.RegistroOriginalId == original.Id)
            .SumAsync(volume => (decimal?)volume.Quantidade, cancellationToken) ?? 0m;

        var totalAtual = original.Quantidade + ajustesExistentes;
        if (totalAtual + entrada.Quantidade < 0)
        {
            return Result.Fail<VolumeResposta>(
                new ErroDeConflito(
                    CodigosDeErro.VolumeNegativo,
                    "O ajuste deixaria o total do registro negativo."));
        }

        var organizacao = await _dbContext
            .Organizacoes
            .AsNoTracking()
            .FirstAsync(item => item.Id == organizacaoId, cancellationToken);
        var agora = _relogio.GetUtcNow();
        var fuso = FusoDaOrganizacao.Obter(organizacao);
        var dataLocal = FusoDaOrganizacao.DataLocal(fuso, agora);

        var ajuste = RegistroDeVolume.CriarAjuste(
            organizacaoId,
            original.TipoResiduoId,
            entrada.Quantidade,
            original.Id,
            operadorUsuarioId,
            operadorNome,
            agora,
            dataLocal,
            entrada.Motivo);

        _dbContext.RegistrosDeVolume.Add(ajuste);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(await MapearAsync(ajuste, cancellationToken));
    }

    private async Task<string?> ObterNomeDoOperadorAsync(
        Guid organizacaoId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        return await _dbContext
            .Usuarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(usuario => usuario.Id == usuarioId
                && usuario.OrganizacaoId == organizacaoId
                && usuario.Ativo)
            .Select(usuario => usuario.Nome)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<VolumeResposta>> MapearVariosAsync(
        IReadOnlyCollection<RegistroDeVolume> volumes,
        CancellationToken cancellationToken)
    {
        var tipoIds = volumes.Select(volume => volume.TipoResiduoId).Distinct().ToList();
        var pontoIds = volumes
            .Where(volume => volume.PontoId is not null)
            .Select(volume => volume.PontoId!.Value)
            .Distinct()
            .ToList();
        var originais = volumes
            .Where(volume => volume.RegistroOriginalId is not null)
            .Select(volume => volume.RegistroOriginalId!.Value)
            .Distinct()
            .ToList();

        var tipos = await _dbContext
            .TiposResiduo
            .AsNoTracking()
            .Where(tipo => tipoIds.Contains(tipo.Id))
            .ToDictionaryAsync(tipo => tipo.Id, cancellationToken);

        var pontos = await _dbContext
            .PontosDeColeta
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(ponto => pontoIds.Contains(ponto.Id))
            .ToDictionaryAsync(ponto => ponto.Id, ponto => ponto.Nome, cancellationToken);

        var totaisDosOriginais = new Dictionary<Guid, decimal>();
        if (originais.Count > 0)
        {
            var registros = await _dbContext
                .RegistrosDeVolume
                .AsNoTracking()
                .Where(volume => originais.Contains(volume.Id)
                    || (volume.RegistroOriginalId != null
                        && originais.Contains(volume.RegistroOriginalId.Value)))
                .ToListAsync(cancellationToken);

            foreach (var originalId in originais)
            {
                var original = registros.FirstOrDefault(volume => volume.Id == originalId);
                if (original is null)
                {
                    continue;
                }

                var ajustes = registros
                    .Where(volume => volume.RegistroOriginalId == originalId)
                    .Sum(volume => volume.Quantidade);

                totaisDosOriginais[originalId] = original.Quantidade + ajustes;
            }
        }

        return volumes
            .Select(volume => Mapear(
                volume,
                tipos.GetValueOrDefault(volume.TipoResiduoId),
                volume.PontoId is Guid pontoId ? pontos.GetValueOrDefault(pontoId) : null,
                volume.RegistroOriginalId is Guid originalId
                    ? totaisDosOriginais.GetValueOrDefault(originalId)
                    : null))
            .ToList();
    }

    private async Task<VolumeResposta> MapearAsync(
        RegistroDeVolume volume,
        CancellationToken cancellationToken)
    {
        var tipo = await _dbContext
            .TiposResiduo
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == volume.TipoResiduoId, cancellationToken);

        string? pontoNome = null;
        if (volume.PontoId is Guid pontoId)
        {
            pontoNome = await _dbContext
                .PontosDeColeta
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(ponto => ponto.Id == pontoId)
                .Select(ponto => ponto.Nome)
                .FirstOrDefaultAsync(cancellationToken);
        }

        decimal? totalOriginal = null;
        if (volume.RegistroOriginalId is Guid originalId)
        {
            var original = await _dbContext
                .RegistrosDeVolume
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == originalId, cancellationToken);

            if (original is not null)
            {
                var ajustes = await _dbContext
                    .RegistrosDeVolume
                    .AsNoTracking()
                    .Where(item => item.RegistroOriginalId == originalId)
                    .SumAsync(item => (decimal?)item.Quantidade, cancellationToken) ?? 0m;

                totalOriginal = original.Quantidade + ajustes;
            }
        }

        return Mapear(volume, tipo, pontoNome, totalOriginal);
    }

    private static VolumeResposta Mapear(
        RegistroDeVolume volume,
        TipoResiduo? tipo,
        string? pontoNome,
        decimal? totalOriginal)
    {
        return new VolumeResposta(
            volume.Id,
            volume.TipoResiduoId,
            tipo?.Codigo ?? string.Empty,
            tipo?.Nome ?? "Tipo removido",
            tipo?.Unidade.ToString() ?? string.Empty,
            volume.Quantidade,
            volume.Origem.ToString(),
            volume.SolicitacaoId,
            volume.PontoId,
            pontoNome,
            volume.RegistroOriginalId,
            totalOriginal,
            volume.OperadorUsuarioId,
            volume.OperadorNome,
            volume.OcorridoEmUtc,
            volume.DataLocal,
            volume.Motivo);
    }
}
