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
    private readonly IFontePublicaDePontos _fontePublica;

    public ConsultaPublicaDePontos(
        ColetaSeletivaDbContext dbContext,
        TimeProvider relogio,
        IFontePublicaDePontos fontePublica)
    {
        _dbContext = dbContext;
        _relogio = relogio;
        _fontePublica = fontePublica;
    }

    public async Task<PaginaDePontosPublicos> BuscarAsync(
        BuscaPublicaDePontosEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        // Os filtros são aplicados sobre a entidade, antes da projeção: predicados sobre
        // um tipo projetado (com entidades dentro) não são traduzidos para SQL e quebravam
        // a busca com qualquer filtro.
        var pontosFiltrados = _dbContext.PontosDeColeta
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(ponto => ponto.Status == StatusPonto.Ativo && ponto.ExcluidoEmUtc == null);

        if (entrada.MunicipioId is int municipioId)
        {
            pontosFiltrados = pontosFiltrados.Where(ponto => ponto.MunicipioId == municipioId);
        }

        if (entrada.TipoResiduoId is Guid tipoResiduoId)
        {
            pontosFiltrados = pontosFiltrados.Where(ponto => _dbContext.PontoTipos
                .IgnoreQueryFilters()
                .Any(tipo => tipo.PontoColetaId == ponto.Id
                    && tipo.TipoResiduoId == tipoResiduoId));
        }

        Point? origem = null;
        if (entrada.Latitude is double latitude && entrada.Longitude is double longitude)
        {
            origem = new Point(longitude, latitude) { SRID = PontoDeColeta.SridWgs84 };
            var raioEmMetros = (entrada.RaioKm ?? 10) * 1000;
            pontosFiltrados = pontosFiltrados.Where(ponto =>
                ponto.Localizacao.IsWithinDistance(origem, raioEmMetros));
        }

        var consulta = from ponto in pontosFiltrados
                       join organizacao in _dbContext.Organizacoes
                           .IgnoreQueryFilters()
                           .AsNoTracking()
                           .Where(organizacao => organizacao.Publicada)
                           on ponto.OrganizacaoId equals organizacao.Id
                       join municipio in _dbContext.Municipios.AsNoTracking()
                           on ponto.MunicipioId equals municipio.Id
                       select new ItemDaConsulta(ponto, organizacao, municipio);

        var candidatos = await consulta
            .Take(LimiteDeCandidatos)
            .ToListAsync(cancellationToken);

        var pontos = candidatos.Select(item => item.Ponto).ToList();
        var horarios = await ObterHorariosAsync(pontos, cancellationToken);
        var tipos = await ObterTiposAsync(pontos, cancellationToken);

        var itens = candidatos
            .Select(item => Mapear(item, horarios, tipos, origem))
            .Where(item => entrada.AbertoAgora != true || item.AbertoAgora == true)
            .ToList();

        var resultadoDaFonte = await BuscarFonteExternaAsync(entrada, origem, cancellationToken);
        if (resultadoDaFonte.Pontos.Count > 0)
        {
            var externos = await MapearPontosExternosAsync(entrada, resultadoDaFonte, origem, cancellationToken);
            itens.AddRange(externos);
        }

        var itensOrdenados = itens
            .OrderBy(item => item.DistanciaKm ?? double.MaxValue)
            .ThenBy(item => item.OrganizacaoNome, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Nome, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToList();

        var total = itensOrdenados.Count;
        var itensDaPagina = itensOrdenados
            .Skip((entrada.Pagina - 1) * entrada.TamanhoPagina)
            .Take(entrada.TamanhoPagina)
            .ToList();

        var temExterno = itensOrdenados.Any(item => item.Origem == OrigemDoPonto.OpenStreetMap);

        return new PaginaDePontosPublicos(
            itensDaPagina,
            entrada.Pagina,
            entrada.TamanhoPagina,
            total,
            AvisoDeFonteExterna: resultadoDaFonte.Aviso,
            AtribuicaoDeFonteExterna: temExterno ? MapeadorDoOpenStreetMap.Atribuicao : null);
    }

    private async Task<ResultadoDaFontePublica> BuscarFonteExternaAsync(
        BuscaPublicaDePontosEntrada entrada,
        Point? origem,
        CancellationToken cancellationToken)
    {
        // Horário dos pontos externos não é confiável na fonte; quando o usuário pede
        // "aberto agora", apenas os pontos cadastrados são considerados.
        if (entrada.AbertoAgora == true)
        {
            return new ResultadoDaFontePublica([], Consultada: false, Disponivel: false, Aviso: null);
        }

        if (_fontePublica is FontePublicaDesativada)
        {
            return new ResultadoDaFontePublica([], Consultada: false, Disponivel: false, Aviso: null);
        }

        string? nomeDoMunicipio = null;
        string? uf = null;

        if (entrada.MunicipioId is int municipioId)
        {
            var municipio = await _dbContext.Municipios
                .AsNoTracking()
                .Where(item => item.Id == municipioId)
                .Select(item => new { item.Nome, item.Uf })
                .FirstOrDefaultAsync(cancellationToken);

            if (municipio is null)
            {
                return new ResultadoDaFontePublica([], Consultada: false, Disponivel: false, Aviso: null);
            }

            nomeDoMunicipio = municipio.Nome;
            uf = municipio.Uf;
        }

        if (nomeDoMunicipio is null && origem is null)
        {
            // Sem município nem proximidade não há recorte geográfico: não varremos o país.
            return new ResultadoDaFontePublica([], Consultada: false, Disponivel: false, Aviso: null);
        }

        var codigos = new List<string>();
        if (entrada.TipoResiduoId is Guid tipoResiduoId)
        {
            var codigo = await _dbContext.TiposResiduo
                .AsNoTracking()
                .Where(tipo => tipo.Id == tipoResiduoId)
                .Select(tipo => tipo.Codigo)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(codigo))
            {
                return new ResultadoDaFontePublica([], Consultada: false, Disponivel: false, Aviso: null);
            }

            codigos.Add(codigo);
        }

        return await _fontePublica.BuscarAsync(
            new ConsultaDeFontePublica(
                MunicipioNome: nomeDoMunicipio,
                Uf: uf,
                Latitude: origem?.Y,
                Longitude: origem?.X,
                RaioKm: entrada.RaioKm,
                CodigosDeTipos: codigos),
            cancellationToken);
    }

    private async Task<List<PontoPublico>> MapearPontosExternosAsync(
        BuscaPublicaDePontosEntrada entrada,
        ResultadoDaFontePublica resultado,
        Point? origem,
        CancellationToken cancellationToken)
    {
        var tiposDoCatalogo = await _dbContext.TiposResiduo
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var tiposPorCodigo = tiposDoCatalogo.ToDictionary(
            tipo => tipo.Codigo,
            tipo => new TipoDoPontoResposta(
                tipo.Id,
                tipo.Codigo,
                tipo.Nome,
                tipo.Unidade.ToString()),
            StringComparer.OrdinalIgnoreCase);

        MunicipioDoCatalogo? municipioDoRecorte = null;
        if (entrada.MunicipioId is int municipioId)
        {
            var municipio = await _dbContext.Municipios
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == municipioId, cancellationToken);

            municipioDoRecorte = municipio is null
                ? null
                : new MunicipioDoCatalogo(municipio.Id, municipio.Nome, municipio.Uf);
        }

        // Duplicidade: o mesmo local pode estar cadastrado por uma organização e também
        // mapeado no OpenStreetMap. O cadastro da plataforma tem prioridade.
        var cadastrados = await _dbContext.PontosDeColeta
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(ponto => ponto.Status == StatusPonto.Ativo && ponto.ExcluidoEmUtc == null)
            .Select(ponto => new { ponto.Nome, ponto.Latitude, ponto.Longitude })
            .ToListAsync(cancellationToken);

        var externos = new List<PontoPublico>(resultado.Pontos.Count);

        foreach (var ponto in resultado.Pontos)
        {
            var duplicado = cadastrados.Any(cadastrado =>
                MapeadorDoOpenStreetMap.CorrespondeAPontoCadastrado(
                    ponto.Nome,
                    ponto.Latitude,
                    ponto.Longitude,
                    cadastrado.Nome,
                    cadastrado.Latitude,
                    cadastrado.Longitude));

            if (duplicado)
            {
                continue;
            }

            var tipos = ponto.CodigosDeTipos
                .Where(codigo => tiposPorCodigo.ContainsKey(codigo))
                .Select(codigo => tiposPorCodigo[codigo])
                .OrderBy(tipo => tipo.Nome, StringComparer.OrdinalIgnoreCase)
                .ToList();

            externos.Add(new PontoPublico(
                Id: ponto.Identificador,
                Origem: OrigemDoPonto.OpenStreetMap,
                Nome: ponto.Nome,
                OrganizacaoNome: ponto.Operador ?? "OpenStreetMap",
                ContatoEmail: null,
                ContatoTelefone: null,
                Municipio: municipioDoRecorte,
                Bairro: ponto.Bairro,
                Rua: ponto.Rua,
                Numero: null,
                Complemento: null,
                Cep: null,
                Latitude: ponto.Latitude,
                Longitude: ponto.Longitude,
                Observacoes: ponto.HorarioLivre is null
                    ? null
                    : $"Horário informado na fonte: {ponto.HorarioLivre}",
                AbertoAgora: null,
                DistanciaKm: origem is null
                    ? null
                    : Math.Round(
                        MapeadorDoOpenStreetMap.DistanciaEmKm(
                            origem.Y,
                            origem.X,
                            ponto.Latitude,
                            ponto.Longitude),
                        2),
                Horarios: [],
                Tipos: tipos,
                Atribuicao: ponto.Atribuicao,
                UrlDaFonte: ponto.UrlDoRegistro));
        }

        return externos;
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
            Id: ponto.Id.ToString(),
            Origem: OrigemDoPonto.Organizacao,
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
