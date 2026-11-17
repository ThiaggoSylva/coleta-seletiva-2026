using System.Globalization;
using System.Text;

namespace ColetaSeletiva.Application.ConsultaPublica;

/// <summary>
/// Mapeamento determinístico das tags do OpenStreetMap para o catálogo de resíduos
/// da plataforma. Puro e sem dependências de infraestrutura para ser testável.
/// </summary>
public static class MapeadorDoOpenStreetMap
{
    public const string Atribuicao = "Pontos externos: © OpenStreetMap contributors (ODbL)";

    public const string NomePadrao = "Ponto de coleta reciclável";

    private static readonly IReadOnlyDictionary<string, string> TagsParaCodigo =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["recycling:paper"] = "papel-e-papelao",
            ["recycling:cardboard"] = "papel-e-papelao",
            ["recycling:newspaper"] = "papel-e-papelao",
            ["recycling:plastic"] = "plastico",
            ["recycling:plastic_bottles"] = "plastico",
            ["recycling:plastic_packaging"] = "plastico",
            ["recycling:glass"] = "vidro",
            ["recycling:glass_bottles"] = "vidro",
            ["recycling:metal"] = "metal",
            ["recycling:cans"] = "metal",
            ["recycling:scrap_metal"] = "metal",
            ["recycling:organic"] = "organico",
            ["recycling:green_waste"] = "organico",
            ["recycling:cooking_oil"] = "oleo-de-cozinha",
            ["recycling:oil"] = "oleo-de-cozinha",
            ["recycling:electrical_items"] = "eletronicos",
            ["recycling:small_appliances"] = "eletronicos",
            ["recycling:e-waste"] = "eletronicos",
            ["recycling:computers"] = "eletronicos",
            ["recycling:batteries"] = "pilhas-e-baterias",
            ["recycling:fluorescent_tubes"] = "lampadas",
            ["recycling:low_energy_bulbs"] = "lampadas",
            ["recycling:medicine"] = "medicamentos",
        };

    /// <summary>Converte as tags de um elemento OSM nos códigos de resíduo atendidos.</summary>
    public static IReadOnlyList<string> CodigosDeTipos(IReadOnlyDictionary<string, string>? tags)
    {
        if (tags is null || tags.Count == 0)
        {
            return [];
        }

        var codigos = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var (chave, valor) in tags)
        {
            if (!TagsParaCodigo.TryGetValue(chave, out var codigo))
            {
                continue;
            }

            if (valor is null
                || valor.Equals("no", StringComparison.OrdinalIgnoreCase)
                || valor.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            codigos.Add(codigo);
        }

        return codigos.ToList();
    }

    /// <summary>Deriva um nome apresentável a partir das tags disponíveis.</summary>
    public static string Nome(IReadOnlyDictionary<string, string>? tags)
    {
        if (tags is null)
        {
            return NomePadrao;
        }

        if (tags.TryGetValue("name", out var nome) && !string.IsNullOrWhiteSpace(nome))
        {
            return nome.Trim();
        }

        if (tags.TryGetValue("operator", out var operador) && !string.IsNullOrWhiteSpace(operador))
        {
            return $"Ponto de coleta — {operador.Trim()}";
        }

        return NomePadrao;
    }

    /// <summary>Normaliza um texto para comparação de duplicidade (sem acentos e caixa).</summary>
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var decomposto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var construtor = new StringBuilder(decomposto.Length);

        foreach (var caractere in decomposto)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(caractere);
            if (categoria == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            construtor.Append(char.IsLetterOrDigit(caractere) ? caractere : ' ');
        }

        return string.Join(
            ' ',
            construtor.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Distância geodésica aproximada em quilômetros (Haversine).</summary>
    public static double DistanciaEmKm(
        double latitudeOrigem,
        double longitudeOrigem,
        double latitudeDestino,
        double longitudeDestino)
    {
        const double RaioDaTerraKm = 6371.0088;

        var lat1 = GrausParaRadianos(latitudeOrigem);
        var lat2 = GrausParaRadianos(latitudeDestino);
        var deltaLat = GrausParaRadianos(latitudeDestino - latitudeOrigem);
        var deltaLon = GrausParaRadianos(longitudeDestino - longitudeOrigem);

        var a = Math.Pow(Math.Sin(deltaLat / 2), 2)
            + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(deltaLon / 2), 2);

        return RaioDaTerraKm * 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    /// <summary>Considera duplicado um ponto externo a menos de 120 m de um cadastrado com o mesmo nome.</summary>
    public static bool CorrespondeAPontoCadastrado(
        string nomeExterno,
        double latitudeExterna,
        double longitudeExterna,
        string nomeCadastrado,
        double latitudeCadastrada,
        double longitudeCadastrada)
    {
        var nomesIguais = Normalizar(nomeExterno) == Normalizar(nomeCadastrado);

        return nomesIguais
            && DistanciaEmKm(latitudeExterna, longitudeExterna, latitudeCadastrada, longitudeCadastrada) <= 0.12;
    }

    private static double GrausParaRadianos(double graus) => graus * Math.PI / 180;
}
