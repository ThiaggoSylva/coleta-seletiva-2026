using ColetaSeletiva.Application.Catalogo;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Pontos;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure.Persistencia;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.Infrastructure.Pontos;

internal sealed class PontosDeColetaService : IPontosDeColeta
{
    private const string IndiceDeNomeUnico = "UX_PontoColeta_OrganizacaoId_Nome";

    private readonly ColetaSeletivaDbContext _dbContext;
    private readonly TimeProvider _relogio;

    public PontosDeColetaService(
        ColetaSeletivaDbContext dbContext,
        TimeProvider relogio)
    {
        _dbContext = dbContext;
        _relogio = relogio;
    }

    public async Task<IReadOnlyList<PontoResumo>> ListarAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken = default)
    {
        var fuso = await ObterFusoAsync(organizacaoId, cancellationToken);
        var pontos = await _dbContext
            .PontosDeColeta
            .AsNoTracking()
            .Include(ponto => ponto.Horarios)
            .Include(ponto => ponto.Tipos)
            .Where(ponto => ponto.OrganizacaoId == organizacaoId && ponto.ExcluidoEmUtc == null)
            .OrderBy(ponto => ponto.Nome)
            .ToListAsync(cancellationToken);

        var municipios = await ObterMunicipiosAsync(
            pontos.Select(ponto => ponto.MunicipioId).Distinct().ToList(),
            cancellationToken);

        var nomesDeTipos = await ObterNomesDeTiposAsync(cancellationToken);

        return pontos
            .Select(ponto => new PontoResumo(
                Id: ponto.Id,
                Nome: ponto.Nome,
                Municipio: municipios.GetValueOrDefault(ponto.MunicipioId, string.Empty),
                Uf: ponto.Uf,
                Status: ponto.Status.ToString(),
                AbertoAgora: ponto.EstaAberto(_relogio.GetUtcNow(), fuso),
                Excluido: ponto.ExcluidoEmUtc is not null,
                Tipos: ponto.Tipos
                    .Select(tipo => nomesDeTipos.GetValueOrDefault(tipo.TipoResiduoId, string.Empty))
                    .Where(nome => nome.Length > 0)
                    .Order()
                    .ToList()))
            .ToList();
    }

    public async Task<Result<PontoDetalhe>> ObterAsync(
        Guid organizacaoId,
        Guid pontoId,
        CancellationToken cancellationToken = default)
    {
        var ponto = await _dbContext
            .PontosDeColeta
            .AsNoTracking()
            .Include(item => item.Horarios)
            .Include(item => item.Tipos)
            .FirstOrDefaultAsync(
                item => item.Id == pontoId && item.OrganizacaoId == organizacaoId,
                cancellationToken);

        if (ponto is null)
        {
            return Result.Fail<PontoDetalhe>(PontoNaoEncontrado());
        }

        var fuso = await ObterFusoAsync(organizacaoId, cancellationToken);
        var municipios = await ObterMunicipiosAsync([ponto.MunicipioId], cancellationToken);
        var tipos = await ObterTiposAsync(
            ponto.Tipos.Select(tipo => tipo.TipoResiduoId).ToList(),
            cancellationToken);

        return Result.Ok(new PontoDetalhe(
            Id: ponto.Id,
            Nome: ponto.Nome,
            Municipio: new MunicipioDoCatalogo(
                ponto.MunicipioId,
                municipios.GetValueOrDefault(ponto.MunicipioId, string.Empty),
                ponto.Uf),
            Uf: ponto.Uf,
            Bairro: ponto.Bairro,
            Rua: ponto.Rua,
            Numero: ponto.Numero,
            Complemento: ponto.Complemento,
            Cep: ponto.Cep,
            Latitude: ponto.Latitude,
            Longitude: ponto.Longitude,
            Observacoes: ponto.Observacoes,
            Status: ponto.Status.ToString(),
            AbertoAgora: ponto.EstaAberto(_relogio.GetUtcNow(), fuso),
            Excluido: ponto.ExcluidoEmUtc is not null,
            Horarios: ponto.Horarios
                .OrderBy(horario => horario.DiaDaSemana)
                .ThenBy(horario => horario.HoraInicio)
                .Select(horario => new HorarioDoPontoResposta(
                    horario.DiaDaSemana.ToString(),
                    ConversaoDePonto.FormatarHora(horario.HoraInicio),
                    ConversaoDePonto.FormatarHora(horario.HoraFim)))
                .ToList(),
            Tipos: tipos));
    }

    public async Task<Result<PontoDetalhe>> CriarAsync(
        Guid organizacaoId,
        CriarPontoEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        var validacao = await ValidarVinculosAsync(
            organizacaoId,
            entrada.MunicipioId,
            entrada.TiposResiduoIds,
            cancellationToken);

        if (validacao.IsFailed)
        {
            return Result.Fail<PontoDetalhe>(validacao.Errors);
        }

        var ponto = PontoDeColeta.Criar(
            nome: entrada.Nome,
            municipioId: entrada.MunicipioId,
            uf: validacao.Value.Uf,
            bairro: entrada.Bairro,
            rua: entrada.Rua,
            numero: entrada.Numero,
            complemento: entrada.Complemento,
            cep: entrada.Cep,
            latitude: entrada.Latitude,
            longitude: entrada.Longitude,
            observacoes: entrada.Observacoes,
            status: ConversaoDePonto.ConverterStatus(entrada.Status),
            horarios: entrada.Horarios.Select(ConversaoDePonto.ConverterHorario).ToList(),
            tiposResiduoIds: entrada.TiposResiduoIds,
            agora: _relogio.GetUtcNow());

        _dbContext.PontosDeColeta.Add(ponto);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (EhNomeDuplicado(excecao))
        {
            return Result.Fail<PontoDetalhe>(NomeEmUso());
        }

        return await ObterAsync(organizacaoId, ponto.Id, cancellationToken);
    }

    public async Task<Result<PontoDetalhe>> AtualizarAsync(
        Guid organizacaoId,
        Guid pontoId,
        AtualizarPontoEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        var ponto = await _dbContext
            .PontosDeColeta
            .Include(item => item.Horarios)
            .Include(item => item.Tipos)
            .FirstOrDefaultAsync(
                item => item.Id == pontoId && item.OrganizacaoId == organizacaoId,
                cancellationToken);

        if (ponto is null)
        {
            return Result.Fail<PontoDetalhe>(PontoNaoEncontrado());
        }

        var validacao = await ValidarVinculosAsync(
            organizacaoId,
            entrada.MunicipioId,
            entrada.TiposResiduoIds,
            cancellationToken);

        if (validacao.IsFailed)
        {
            return Result.Fail<PontoDetalhe>(validacao.Errors);
        }

        try
        {
            ponto.Atualizar(
                nome: entrada.Nome,
                municipioId: entrada.MunicipioId,
                uf: validacao.Value.Uf,
                bairro: entrada.Bairro,
                rua: entrada.Rua,
                numero: entrada.Numero,
                complemento: entrada.Complemento,
                cep: entrada.Cep,
                latitude: entrada.Latitude,
                longitude: entrada.Longitude,
                observacoes: entrada.Observacoes,
                status: ConversaoDePonto.ConverterStatus(entrada.Status),
                horarios: entrada.Horarios.Select(ConversaoDePonto.ConverterHorario).ToList(),
                tiposResiduoIds: entrada.TiposResiduoIds,
                agora: _relogio.GetUtcNow());

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (EhNomeDuplicado(excecao))
        {
            return Result.Fail<PontoDetalhe>(NomeEmUso());
        }

        return await ObterAsync(organizacaoId, pontoId, cancellationToken);
    }

    public async Task<Result<PontoDetalhe>> AlterarStatusAsync(
        Guid organizacaoId,
        Guid pontoId,
        StatusPonto status,
        CancellationToken cancellationToken = default)
    {
        var ponto = await _dbContext
            .PontosDeColeta
            .FirstOrDefaultAsync(
                item => item.Id == pontoId && item.OrganizacaoId == organizacaoId,
                cancellationToken);

        if (ponto is null)
        {
            return Result.Fail<PontoDetalhe>(PontoNaoEncontrado());
        }

        try
        {
            ponto.AlterarStatus(status, _relogio.GetUtcNow());
        }
        catch (InvalidOperationException)
        {
            return Result.Fail<PontoDetalhe>(
                new ErroDeConflito(
                    CodigosDeErro.PontoRetirado,
                    "Ponto retirado logicamente não pode ser alterado."));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ObterAsync(organizacaoId, pontoId, cancellationToken);
    }

    public async Task<Result<PontoDetalhe>> RetirarAsync(
        Guid organizacaoId,
        Guid pontoId,
        CancellationToken cancellationToken = default)
    {
        var ponto = await _dbContext
            .PontosDeColeta
            .FirstOrDefaultAsync(
                item => item.Id == pontoId && item.OrganizacaoId == organizacaoId,
                cancellationToken);

        if (ponto is null)
        {
            return Result.Fail<PontoDetalhe>(PontoNaoEncontrado());
        }

        ponto.Retirar(_relogio.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ObterAsync(organizacaoId, pontoId, cancellationToken);
    }

    public async Task<IReadOnlyList<OpcaoDePonto>> ListarOpcoesAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken = default)
    {
        return await (
                from ponto in _dbContext.PontosDeColeta.AsNoTracking()
                join municipio in _dbContext.Municipios.AsNoTracking()
                    on ponto.MunicipioId equals municipio.Id
                where ponto.OrganizacaoId == organizacaoId
                    && ponto.Status == StatusPonto.Ativo
                    && ponto.ExcluidoEmUtc == null
                orderby ponto.Nome
                select new OpcaoDePonto(
                    ponto.Id,
                    ponto.Nome,
                    municipio.Nome,
                    ponto.Uf,
                    _dbContext
                        .PontoTipos.AsNoTracking()
                        .Where(tipo => tipo.PontoColetaId == ponto.Id)
                        .Select(tipo => tipo.TipoResiduoId)
                        .ToList()))
            .ToListAsync(cancellationToken);
    }

    private async Task<TimeZoneInfo> ObterFusoAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken)
    {
        var fuso = await _dbContext
            .Organizacoes
            .AsNoTracking()
            .Where(organizacao => organizacao.Id == organizacaoId)
            .Select(organizacao => organizacao.Fuso)
            .FirstOrDefaultAsync(cancellationToken);

        return TimeZoneInfo.FindSystemTimeZoneById(fuso ?? Organizacao.FusoPadrao);
    }

    private async Task<Dictionary<int, string>> ObterMunicipiosAsync(
        IReadOnlyCollection<int> municipioIds,
        CancellationToken cancellationToken)
    {
        return await _dbContext
            .Municipios
            .AsNoTracking()
            .Where(municipio => municipioIds.Contains(municipio.Id))
            .ToDictionaryAsync(
                municipio => municipio.Id,
                municipio => municipio.Nome,
                cancellationToken);
    }

    private async Task<Dictionary<Guid, string>> ObterNomesDeTiposAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext
            .TiposResiduo
            .AsNoTracking()
            .ToDictionaryAsync(tipo => tipo.Id, tipo => tipo.Nome, cancellationToken);
    }

    private async Task<IReadOnlyList<TipoDoPontoResposta>> ObterTiposAsync(
        IReadOnlyCollection<Guid> tiposResiduoIds,
        CancellationToken cancellationToken)
    {
        var tipos = await _dbContext
            .TiposResiduo
            .AsNoTracking()
            .Where(tipo => tiposResiduoIds.Contains(tipo.Id))
            .OrderBy(tipo => tipo.Nome)
            .ToListAsync(cancellationToken);

        return tipos
            .Select(tipo => new TipoDoPontoResposta(
                tipo.Id,
                tipo.Codigo,
                tipo.Nome,
                tipo.Unidade.ToString()))
            .ToList();
    }

    private async Task<Result<VinculosDoPonto>> ValidarVinculosAsync(
        Guid organizacaoId,
        int municipioId,
        IReadOnlyCollection<Guid> tiposResiduoIds,
        CancellationToken cancellationToken)
    {
        var municipio = await (
                from vinculo in _dbContext.OrganizacaoMunicipios.AsNoTracking()
                join municipioAtendido in _dbContext.Municipios.AsNoTracking()
                    on vinculo.MunicipioId equals municipioAtendido.Id
                where vinculo.OrganizacaoId == organizacaoId
                    && vinculo.MunicipioId == municipioId
                select municipioAtendido)
            .FirstOrDefaultAsync(cancellationToken);

        if (municipio is null)
        {
            return Result.Fail<VinculosDoPonto>(
                new ErroDeValidacao(
                    "municipioId",
                    "O município do ponto deve constar entre os municípios atendidos."));
        }

        var habilitados = await _dbContext
            .OrganizacaoTipos
            .AsNoTracking()
            .Where(vinculo => vinculo.OrganizacaoId == organizacaoId)
            .Select(vinculo => vinculo.TipoResiduoId)
            .ToListAsync(cancellationToken);

        var naoHabilitados = tiposResiduoIds.Except(habilitados).ToList();
        if (naoHabilitados.Count > 0)
        {
            return Result.Fail<VinculosDoPonto>(
                new ErroDeValidacao(
                    "tiposResiduoIds",
                    "Todos os tipos aceitos devem estar habilitados pela organização."));
        }

        return Result.Ok(new VinculosDoPonto(municipio.Uf));
    }

    private static bool EhNomeDuplicado(DbUpdateException excecao)
    {
        return excecao.InnerException is Microsoft.Data.SqlClient.SqlException sql
            && (sql.Number == 2601 || sql.Number == 2627)
            && sql.Message.Contains(IndiceDeNomeUnico, StringComparison.Ordinal);
    }

    private static ErroNaoEncontrado PontoNaoEncontrado()
    {
        return new ErroNaoEncontrado("Ponto de coleta não encontrado.");
    }

    private static ErroDeConflito NomeEmUso()
    {
        return new ErroDeConflito(
            CodigosDeErro.NomeEmUso,
            "Já existe um ponto com este nome na organização.");
    }

    private sealed record VinculosDoPonto(string Uf);
}
