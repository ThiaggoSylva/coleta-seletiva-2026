using ColetaSeletiva.Application.Catalogo;
using ColetaSeletiva.Application.ConsultaPublica;
using ColetaSeletiva.Application.Pontos;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace ColetaSeletiva.Infrastructure.ConsultaPublica;

internal sealed class ConsultaPublicaDePontos : IConsultaPublicaDePontos
{
    private const int LimiteDeCandidatos = 500;

    private readonly ColetaSeletivaDbContext _dbContext;
    private readonly TimeProvider _relogio;

    public ConsultaPublicaDePontos(
        ColetaSeletivaDbContext dbContext,
        TimeProvider relogio)
    {
        _dbContext = dbContext;
        _relogio = relogio;
    }

    public async Task<PaginaDePontosPublicos> BuscarAsync(
        BuscaPublicaDePontosEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var consulta = from ponto in _dbContext.PontosDeColeta
                           .IgnoreQueryFilters()
                           .AsNoTracking()
                       join organizacao in _dbContext.Organizacoes
                           .IgnoreQueryFilters()
                           .AsNoTracking()
                           on ponto.OrganizacaoId equals organizacao.Id
                       join municipio in _dbContext.Municipios.AsNoTracking()
                           on ponto.MunicipioId equals municipio.Id
                       where organizacao.Publicada
                           && ponto.Status == StatusPonto.Ativo
                           && ponto.ExcluidoEmUtc == null
                       select new ItemDaConsulta(ponto, organizacao, municipio);

        if (entrada.MunicipioId is int municipioId)
        {
            consulta = consulta.Where(item => item.Ponto.MunicipioId == municipioId);
        }

        if (entrada.TipoResiduoId is Guid tipoResiduoId)
        {
            consulta = consulta.Where(item => _dbContext.PontoTipos
                .Any(tipo => tipo.PontoColetaId == item.Ponto.Id
                    && tipo.TipoResiduoId == tipoResiduoId));
        }

        Point? origem = null;
        if (entrada.Latitude is double latitude && entrada.Longitude is double longitude)
        {
            origem = new Point(longitude, latitude) { SRID = PontoDeColeta.SridWgs84 };
            var raioEmMetros = (entrada.RaioKm ?? 10) * 1000;
            consulta = consulta.Where(item =>
                item.Ponto.Localizacao.IsWithinDistance(origem, raioEmMetros));
        }

        var candidatos = await consulta
            .Take(LimiteDeCandidatos)
            .ToListAsync(cancellationToken);

        var pontos = candidatos.Select(item => item.Ponto).ToList();
        var horarios = await ObterHorariosAsync(pontos, cancellationToken);
        var tipos = await ObterTiposAsync(pontos, cancellationToken);

        var itens = candidatos
            .Select(item => Mapear(item, horarios, tipos, origem))
            .Where(item => entrada.AbertoAgora != true || item.AbertoAgora)
            .OrderBy(item => item.DistanciaKm ?? double.MaxValue)
            .ThenBy(item => item.OrganizacaoNome, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Nome, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Id)
            .ToList();

        var total = itens.Count;
        var itensDaPagina = itens
            .Skip((entrada.Pagina - 1) * entrada.TamanhoPagina)
            .Take(entrada.TamanhoPagina)
            .ToList();

        return new PaginaDePontosPublicos(
            itensDaPagina,
            entrada.Pagina,
            entrada.TamanhoPagina,
            total);
    }

    private async Task<ILookup<Guid, PontoHorario>> ObterHorariosAsync(
        IReadOnlyCollection<PontoDeColeta> pontos,
        CancellationToken cancellationToken)
    {
        var ids = pontos.Select(ponto => ponto.Id).ToList();

        var horarios = await _dbContext
            .PontoHorarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(horario => ids.Contains(horario.PontoColetaId))
            .ToListAsync(cancellationToken);

        return horarios.ToLookup(horario => horario.PontoColetaId);
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<TipoDoPontoResposta>>> ObterTiposAsync(
        IReadOnlyCollection<PontoDeColeta> pontos,
        CancellationToken cancellationToken)
    {
        var ids = pontos.Select(ponto => ponto.Id).ToList();

        var vinculos = await (
                from tipo in _dbContext.PontoTipos.IgnoreQueryFilters().AsNoTracking()
                join residuo in _dbContext.TiposResiduo.AsNoTracking()
                    on tipo.TipoResiduoId equals residuo.Id
                where ids.Contains(tipo.PontoColetaId)
                select new
                {
                    tipo.PontoColetaId,
                    Residuo = residuo,
                })
            .ToListAsync(cancellationToken);

        return vinculos
            .GroupBy(vinculo => vinculo.PontoColetaId)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => (IReadOnlyList<TipoDoPontoResposta>)grupo
                    .Select(vinculo => new TipoDoPontoResposta(
                        vinculo.Residuo.Id,
                        vinculo.Residuo.Codigo,
                        vinculo.Residuo.Nome,
                        vinculo.Residuo.Unidade.ToString()))
                    .OrderBy(tipo => tipo.Nome, StringComparer.OrdinalIgnoreCase)
                    .ToList());
    }

    private PontoPublico Mapear(
        ItemDaConsulta item,
        ILookup<Guid, PontoHorario> horarios,
        IReadOnlyDictionary<Guid, IReadOnlyList<TipoDoPontoResposta>> tipos,
        Point? origem)
    {
        var ponto = item.Ponto;
        var organizacao = item.Organizacao;
        var municipio = item.Municipio;

        var fuso = TimeZoneInfo.FindSystemTimeZoneById(organizacao.Fuso);
        var agora = _relogio.GetUtcNow();
        var instanteLocal = TimeZoneInfo.ConvertTime(agora, fuso);
        var horaLocal = TimeOnly.FromDateTime(instanteLocal.DateTime);

        var horariosDoPonto = horarios[ponto.Id]
            .OrderBy(horario => horario.DiaDaSemana)
            .ThenBy(horario => horario.HoraInicio)
            .ToList();

        var abertoAgora = ponto.Status == StatusPonto.Ativo
            && ponto.ExcluidoEmUtc is null
            && horariosDoPonto.Any(horario =>
                horario.DiaDaSemana == instanteLocal.DayOfWeek
                && horaLocal >= horario.HoraInicio
                && horaLocal < horario.HoraFim);

        return new PontoPublico(
            Id: ponto.Id,
            Nome: ponto.Nome,
            OrganizacaoNome: organizacao.Nome,
            ContatoEmail: organizacao.ContatoEmail,
            ContatoTelefone: organizacao.ContatoTelefone,
            Municipio: new MunicipioDoCatalogo(municipio.Id, municipio.Nome, municipio.Uf),
            Bairro: ponto.Bairro,
            Rua: ponto.Rua,
            Numero: ponto.Numero,
            Complemento: ponto.Complemento,
            Cep: ponto.Cep,
            Latitude: ponto.Latitude,
            Longitude: ponto.Longitude,
            Observacoes: ponto.Observacoes,
            AbertoAgora: abertoAgora,
            DistanciaKm: origem is null
                ? null
                : Math.Round(ponto.Localizacao.Distance(origem) / 1000, 2),
            Horarios: horariosDoPonto
                .Select(horario => new HorarioDoPontoResposta(
                    horario.DiaDaSemana.ToString(),
                    horario.HoraInicio.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                    horario.HoraFim.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture)))
                .ToList(),
            Tipos: tipos.TryGetValue(ponto.Id, out var tiposDoPonto) ? tiposDoPonto : []);
    }

    private sealed record ItemDaConsulta(PontoDeColeta Ponto, Organizacao Organizacao, Municipio Municipio);
}
