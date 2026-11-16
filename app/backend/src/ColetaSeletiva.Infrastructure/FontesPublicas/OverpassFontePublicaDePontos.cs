using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;
using ColetaSeletiva.Application.ConsultaPublica;
using Microsoft.Extensions.Logging;

namespace ColetaSeletiva.Infrastructure.FontesPublicas;

/// <summary>
/// Lê pontos de coleta reais do OpenStreetMap. Usa o Nominatim apenas para converter
/// município em caixa geográfica e o Overpass para listar os elementos
/// <c>amenity=recycling</c>. Nada é persistido: a leitura é somente em memória, com cache
/// curto e degradação silenciosa quando a fonte pública está indisponível.
/// </summary>
public sealed class OverpassFontePublicaDePontos : IFontePublicaDePontos
{
    private const string AvisoIndisponivel =
        "Não foi possível consultar os pontos de fontes públicas agora; exibindo apenas os pontos cadastrados.";

    private readonly HttpClient _cliente;
    private readonly ILogger<OverpassFontePublicaDePontos> _logger;
    private readonly OpcoesDaFontePublica _opcoes;
    private readonly TimeProvider _relogio;
    private readonly ConcurrentDictionary<string, EntradaDeCache<ResultadoDaFontePublica>> _cache = new();
    private readonly ConcurrentDictionary<string, EntradaDeCache<CaixaGeografica?>> _cacheDeMunicipios = new();

    public OverpassFontePublicaDePontos(
        HttpClient cliente,
        ILogger<OverpassFontePublicaDePontos> logger,
        OpcoesDaFontePublica opcoes,
        TimeProvider? relogio = null)
    {
        _cliente = cliente;
        _logger = logger;
        _opcoes = opcoes;
        _relogio = relogio ?? TimeProvider.System;
    }

    public async Task<ResultadoDaFontePublica> BuscarAsync(
        ConsultaDeFontePublica consulta,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        if (!_opcoes.Ativa)
        {
            return new ResultadoDaFontePublica([], Consultada: false, Disponivel: false, Aviso: null);
        }

        var chave = ChaveDeCache(consulta);
        if (_cache.TryGetValue(chave, out var cacheada) && cacheada.ExpiraEmUtc > _relogio.GetUtcNow())
        {
            return cacheada.Valor;
        }

        try
        {
            var caixa = await ResolverCaixaAsync(consulta, cancellationToken);
            if (caixa is null)
            {
                return new ResultadoDaFontePublica([], Consultada: true, Disponivel: false, AvisoIndisponivel);
            }

            var texto = await ConsultarOverpassAsync(caixa, consulta, cancellationToken);
            var pontos = Interpretar(texto, consulta);

            var resultado = new ResultadoDaFontePublica(
                pontos,
                Consultada: true,
                Disponivel: true,
                Aviso: null);

            Guardar(_cache, chave, resultado, _opcoes.DuracaoDoCache);
            return resultado;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception excecao) when (excecao is HttpRequestException
            or TaskCanceledException
            or JsonException
            or InvalidOperationException)
        {
            _logger.LogWarning(
                "Fonte pública de pontos indisponível para {Municipio}/{Uf}; a consulta segue apenas com os pontos cadastrados. Tipo: {Tipo}",
                consulta.MunicipioNome ?? "(sem município)",
                consulta.Uf ?? "(sem UF)",
                excecao.GetType().Name);

            return new ResultadoDaFontePublica([], Consultada: true, Disponivel: false, AvisoIndisponivel);
        }
    }

    private static string ChaveDeCache(ConsultaDeFontePublica consulta)
    {
        var tipos = consulta.CodigosDeTipos is { Count: > 0 }
            ? string.Join(',', consulta.CodigosDeTipos.OrderBy(codigo => codigo, StringComparer.Ordinal))
            : "todos";

        if (consulta.Latitude is double latitude && consulta.Longitude is double longitude)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"raio:{latitude:F3}:{longitude:F3}:{consulta.RaioKm ?? 10:F1}:{tipos}");
        }

        return $"municipio:{MapeadorDoOpenStreetMap.Normalizar(consulta.MunicipioNome)}:{consulta.Uf?.ToUpperInvariant()}:{tipos}";
    }

    private async Task<CaixaGeografica?> ResolverCaixaAsync(
        ConsultaDeFontePublica consulta,
        CancellationToken cancellationToken)
    {
        if (consulta.Latitude is double latitude && consulta.Longitude is double longitude)
        {
            var raio = Math.Max(0.5, consulta.RaioKm ?? 10);
            var deltaLat = raio / 111.32;
            var deltaLon = raio / (111.32 * Math.Cos(latitude * Math.PI / 180));

            return new CaixaGeografica(
                latitude - deltaLat,
                longitude - deltaLon,
                latitude + deltaLat,
                longitude + deltaLon);
        }

        if (string.IsNullOrWhiteSpace(consulta.MunicipioNome) || string.IsNullOrWhiteSpace(consulta.Uf))
        {
            return null;
        }

        var chave = $"{MapeadorDoOpenStreetMap.Normalizar(consulta.MunicipioNome)}:{consulta.Uf}";
        if (_cacheDeMunicipios.TryGetValue(chave, out var emCache)
            && emCache.ExpiraEmUtc > _relogio.GetUtcNow())
        {
            return emCache.Valor;
        }

        var termo = Uri.EscapeDataString(
            $"{consulta.MunicipioNome}, {consulta.Uf}, Brasil");

        var url = $"{_opcoes.UrlDoNominatim}?q={termo}&format=jsonv2&limit=1&countrycodes=br";
        if (!string.IsNullOrWhiteSpace(_opcoes.EmailDeContato))
        {
            url += $"&email={Uri.EscapeDataString(_opcoes.EmailDeContato)}";
        }

        using var requisicao = new HttpRequestMessage(HttpMethod.Get, url);
        using var resposta = await _cliente.SendAsync(requisicao, cancellationToken);
        resposta.EnsureSuccessStatusCode();

        var corpo = await resposta.Content.ReadAsStringAsync(cancellationToken);
        var caixa = InterpretarCaixaDoNominatim(corpo);

        Guardar(_cacheDeMunicipios, chave, caixa, _opcoes.DuracaoDoCacheDoMunicipio);
        return caixa;
    }

    private static CaixaGeografica? InterpretarCaixaDoNominatim(string corpo)
    {
        using var documento = JsonDocument.Parse(corpo);
        if (documento.RootElement.ValueKind != JsonValueKind.Array
            || documento.RootElement.GetArrayLength() == 0)
        {
            return null;
        }

        var primeiro = documento.RootElement[0];
        if (!primeiro.TryGetProperty("boundingbox", out var caixa)
            || caixa.ValueKind != JsonValueKind.Array
            || caixa.GetArrayLength() != 4)
        {
            return null;
        }

        var valores = caixa.EnumerateArray()
            .Select(itemo => double.Parse(itemo.GetString() ?? "0", CultureInfo.InvariantCulture))
            .ToList();

        // Nominatim devolve [sul, norte, oeste, leste].
        return new CaixaGeografica(valores[0], valores[2], valores[1], valores[3]);
    }

    private async Task<string> ConsultarOverpassAsync(
        CaixaGeografica caixa,
        ConsultaDeFontePublica consulta,
        CancellationToken cancellationToken)
    {
        var limite = Math.Clamp(consulta.MaximoDePontos, 1, _opcoes.LimiteDePontos);
        var segundos = Math.Max(5, (int)_opcoes.Timeout.TotalSeconds);

        var consultaOverpass = $"""
[out:json][timeout:{segundos}];
nwr["amenity"="recycling"]({Formatar(caixa.Sul)},{Formatar(caixa.Oeste)},{Formatar(caixa.Norte)},{Formatar(caixa.Leste)});
out center tags {limite};
""";

        using var conteudo = new FormUrlEncodedContent(
            [new KeyValuePair<string, string>("data", consultaOverpass)]);

        using var resposta = await _cliente.PostAsync(_opcoes.UrlDoOverpass, conteudo, cancellationToken);
        resposta.EnsureSuccessStatusCode();

        return await resposta.Content.ReadAsStringAsync(cancellationToken);
    }

    private IReadOnlyList<PontoDeFontePublica> Interpretar(
        string corpo,
        ConsultaDeFontePublica consulta)
    {
        using var documento = JsonDocument.Parse(corpo);

        if (!documento.RootElement.TryGetProperty("elements", out var elementos)
            || elementos.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var pontos = new List<PontoDeFontePublica>(elementos.GetArrayLength());
        var filtroDeTipos = consulta.CodigosDeTipos ?? [];

        foreach (var elemento in elementos.EnumerateArray())
        {
            var latitude = LerCoordenada(elemento, "lat", "center");
            var longitude = LerCoordenada(elemento, "lon", "center");

            if (latitude is not double lat || longitude is not double lon)
            {
                continue;
            }

            var tipo = elemento.TryGetProperty("type", out var tipoElemento)
                ? tipoElemento.GetString() ?? "node"
                : "node";
            var identificador = elemento.TryGetProperty("id", out var idElemento)
                ? idElemento.GetInt64().ToString(CultureInfo.InvariantCulture)
                : Guid.NewGuid().ToString("N");

            var tags = LerTags(elemento);
            var codigos = MapeadorDoOpenStreetMap.CodigosDeTipos(tags);

            if (filtroDeTipos.Count > 0 && !codigos.Any(codigo => filtroDeTipos.Contains(codigo)))
            {
                continue;
            }

            pontos.Add(new PontoDeFontePublica(
                Identificador: $"osm:{tipo}/{identificador}",
                Nome: MapeadorDoOpenStreetMap.Nome(tags),
                Operador: LerTag(tags, "operator"),
                Latitude: lat,
                Longitude: lon,
                Rua: LerTag(tags, "addr:street"),
                Bairro: LerTag(tags, "addr:suburb"),
                HorarioLivre: LerTag(tags, "opening_hours"),
                CodigosDeTipos: codigos,
                UrlDoRegistro: $"https://www.openstreetmap.org/{tipo}/{identificador}",
                Atribuicao: MapeadorDoOpenStreetMap.Atribuicao));

            if (pontos.Count >= _opcoes.LimiteDePontos)
            {
                break;
            }
        }

        return pontos;
    }

    private static double? LerCoordenada(JsonElement elemento, string nome, string nomeDoCentro)
    {
        if (elemento.TryGetProperty(nome, out var direto) && direto.TryGetDouble(out var valor))
        {
            return valor;
        }

        if (elemento.TryGetProperty(nomeDoCentro, out var centro)
            && centro.ValueKind == JsonValueKind.Object
            && centro.TryGetProperty(nome, out var aninhado)
            && aninhado.TryGetDouble(out var valorAninhado))
        {
            return valorAninhado;
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string> LerTags(JsonElement elemento)
    {
        if (!elemento.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var dicionario = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in tags.EnumerateObject())
        {
            if (tag.Value.ValueKind == JsonValueKind.String)
            {
                dicionario[tag.Name] = tag.Value.GetString() ?? string.Empty;
            }
        }

        return dicionario;
    }

    private static string? LerTag(IReadOnlyDictionary<string, string> tags, string chave)
    {
        return tags.TryGetValue(chave, out var valor) && !string.IsNullOrWhiteSpace(valor)
            ? valor.Trim()
            : null;
    }

    private static string Formatar(double valor) =>
        valor.ToString("0.######", CultureInfo.InvariantCulture);

    private void Guardar<T>(
        ConcurrentDictionary<string, EntradaDeCache<T>> cache,
        string chave,
        T valor,
        TimeSpan duracao)
    {
        var agora = _relogio.GetUtcNow();

        foreach (var (chaveExistente, entrada) in cache)
        {
            if (entrada.ExpiraEmUtc <= agora)
            {
                cache.TryRemove(chaveExistente, out _);
            }
        }

        cache[chave] = new EntradaDeCache<T>(valor, agora.Add(duracao));
    }

    private sealed record CaixaGeografica(double Sul, double Oeste, double Norte, double Leste);

    private sealed record EntradaDeCache<T>(T Valor, DateTimeOffset ExpiraEmUtc);
}
