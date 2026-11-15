using ColetaSeletiva.Application.Erros;
using FluentResults;

namespace ColetaSeletiva.Application.ConsultaPublica;

public sealed class BuscarPontosPublicosHandler
{
    public const int TamanhoPadraoDaPagina = 20;
    public const int TamanhoMaximoDaPagina = 100;
    public const double RaioPadraoKm = 10;
    public const double RaioMaximoKm = 100;

    private readonly IConsultaPublicaDePontos _consulta;

    public BuscarPontosPublicosHandler(IConsultaPublicaDePontos consulta)
    {
        _consulta = consulta;
    }

    public async Task<Result<PaginaDePontosPublicos>> ExecutarAsync(
        BuscaPublicaDePontosEntrada? entrada,
        CancellationToken cancellationToken = default)
    {
        var normalizada = entrada ?? new BuscaPublicaDePontosEntrada();
        var erros = new List<ErroDeValidacao>();

        var temLatitude = normalizada.Latitude is not null;
        var temLongitude = normalizada.Longitude is not null;

        if (temLatitude != temLongitude)
        {
            erros.Add(new ErroDeValidacao(
                "latitude",
                "Informe latitude e longitude juntas ou omita as duas."));
        }

        if (normalizada.Latitude is double latitude
            && (latitude is < -90 or > 90))
        {
            erros.Add(new ErroDeValidacao("latitude", "Latitude fora do intervalo válido."));
        }

        if (normalizada.Longitude is double longitude
            && (longitude is < -180 or > 180))
        {
            erros.Add(new ErroDeValidacao("longitude", "Longitude fora do intervalo válido."));
        }

        if (normalizada.RaioKm is double raio
            && (raio <= 0 || raio > RaioMaximoKm))
        {
            erros.Add(new ErroDeValidacao(
                "raioKm",
                $"O raio deve ser maior que zero e no máximo {RaioMaximoKm} km."));
        }

        if (normalizada.MunicipioId is int municipioId && municipioId <= 0)
        {
            erros.Add(new ErroDeValidacao("municipioId", "Município inválido."));
        }

        if (normalizada.Pagina < 1)
        {
            erros.Add(new ErroDeValidacao("pagina", "A página deve ser maior ou igual a 1."));
        }

        var tamanhoPagina = normalizada.TamanhoPagina <= 0
            ? TamanhoPadraoDaPagina
            : normalizada.TamanhoPagina;

        if (tamanhoPagina > TamanhoMaximoDaPagina)
        {
            erros.Add(new ErroDeValidacao(
                "tamanhoPagina",
                $"O tamanho da página deve ser no máximo {TamanhoMaximoDaPagina}."));
        }

        if (normalizada.DistanciaInformada())
        {
            normalizada = normalizada with
            {
                RaioKm = normalizada.RaioKm ?? RaioPadraoKm,
            };
        }

        if (erros.Count > 0)
        {
            return Result.Fail<PaginaDePontosPublicos>(erros);
        }

        var pagina = await _consulta.BuscarAsync(
            normalizada with { TamanhoPagina = tamanhoPagina, Pagina = normalizada.Pagina },
            cancellationToken);

        return Result.Ok(pagina);
    }
}

internal static class BuscaPublicaExtensions
{
    public static bool DistanciaInformada(this BuscaPublicaDePontosEntrada entrada)
    {
        return entrada.Latitude is not null && entrada.Longitude is not null;
    }
}
