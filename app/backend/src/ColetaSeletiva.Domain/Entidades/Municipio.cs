namespace ColetaSeletiva.Domain.Entidades;

/// <summary>
/// Município do catálogo global do IBGE, identificado pelo código oficial (DT-15).
/// </summary>
public sealed class Municipio
{
    public const int NomeMaxLength = 120;
    public const int UfLength = 2;

    private Municipio()
    {
    }

    private Municipio(int id, string nome, string uf)
    {
        Id = id;
        Nome = nome;
        Uf = uf;
    }

    /// <summary>Código IBGE do município.</summary>
    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string Uf { get; private set; } = string.Empty;

    public static Municipio Criar(int codigoIbge, string nome, string uf)
    {
        if (codigoIbge <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(codigoIbge),
                codigoIbge,
                "O código IBGE do município deve ser positivo.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(uf);

        var nomeNormalizado = nome.Trim();
        if (nomeNormalizado.Length > NomeMaxLength)
        {
            throw new ArgumentException(
                $"O nome do município deve ter no máximo {NomeMaxLength} caracteres.",
                nameof(nome));
        }

        var ufNormalizada = uf.Trim().ToUpperInvariant();
        if (ufNormalizada.Length != UfLength || !ufNormalizada.All(char.IsLetter))
        {
            throw new ArgumentException("A UF deve ter duas letras.", nameof(uf));
        }

        return new Municipio(codigoIbge, nomeNormalizado, ufNormalizada);
    }
}
