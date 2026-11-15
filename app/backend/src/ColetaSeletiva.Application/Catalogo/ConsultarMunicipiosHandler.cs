using ColetaSeletiva.Application.Erros;
using FluentResults;

namespace ColetaSeletiva.Application.Catalogo;

public sealed class ConsultarMunicipiosHandler
{
    public const int TamanhoMaximoDeTexto = 60;
    public const int LimitePadrao = 50;
    public const int LimiteMaximo = 200;

    private readonly ICatalogoDeMunicipios _catalogo;

    public ConsultarMunicipiosHandler(ICatalogoDeMunicipios catalogo)
    {
        _catalogo = catalogo;
    }

    public async Task<Result<IReadOnlyList<MunicipioDoCatalogo>>> ExecutarAsync(
        string? uf,
        string? texto,
        CancellationToken cancellationToken = default)
    {
        var erros = new List<ErroDeValidacao>();
        var ufNormalizada = uf?.Trim().ToUpperInvariant();

        if (!string.IsNullOrEmpty(ufNormalizada)
            && (ufNormalizada.Length != 2 || !ufNormalizada.All(char.IsLetter)))
        {
            erros.Add(new ErroDeValidacao("uf", "A UF deve ter duas letras."));
        }

        var textoNormalizado = texto?.Trim();
        if (textoNormalizado is { Length: > TamanhoMaximoDeTexto })
        {
            erros.Add(new ErroDeValidacao(
                "texto",
                $"O texto deve ter no máximo {TamanhoMaximoDeTexto} caracteres."));
        }

        if (erros.Count > 0)
        {
            return Result.Fail<IReadOnlyList<MunicipioDoCatalogo>>(erros);
        }

        var municipios = await _catalogo.ListarAsync(
            ufNormalizada,
            string.IsNullOrWhiteSpace(textoNormalizado) ? null : textoNormalizado,
            LimitePadrao,
            cancellationToken);

        return Result.Ok(municipios);
    }
}
