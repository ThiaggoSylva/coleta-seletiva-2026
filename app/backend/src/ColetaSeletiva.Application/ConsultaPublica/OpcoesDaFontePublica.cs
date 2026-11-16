namespace ColetaSeletiva.Application.ConsultaPublica;

/// <summary>Configuração da leitura de pontos em fontes públicas externas.</summary>
public sealed record OpcoesDaFontePublica(
    bool Ativa = false,
    string UrlDoOverpass = "https://overpass-api.de/api/interpreter",
    string UrlDoNominatim = "https://nominatim.openstreetmap.org/search.php",
    string EmailDeContato = "",
    int TimeoutSegundos = 8,
    int MinutosDeCache = 10,
    int MinutosDeCacheDoMunicipio = 1440,
    int MaximoDePontos = 100)
{
    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSegundos <= 0 ? 8 : TimeoutSegundos);

    public TimeSpan DuracaoDoCache =>
        TimeSpan.FromMinutes(MinutosDeCache <= 0 ? 10 : MinutosDeCache);

    public TimeSpan DuracaoDoCacheDoMunicipio =>
        TimeSpan.FromMinutes(MinutosDeCacheDoMunicipio <= 0 ? 1440 : MinutosDeCacheDoMunicipio);

    public int LimiteDePontos => MaximoDePontos <= 0 ? 100 : MaximoDePontos;
}
