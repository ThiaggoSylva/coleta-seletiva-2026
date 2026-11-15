using ColetaSeletiva.Domain.Catalogo;

namespace ColetaSeletiva.Domain.Entidades;

/// <summary>
/// Tipo de resíduo do catálogo global da plataforma. É somente leitura na v1 (RF-07).
/// </summary>
public sealed class TipoResiduo
{
    public const int CodigoMaxLength = 60;
    public const int NomeMaxLength = 120;
    public const int OrientacoesMaxLength = 1000;

    private TipoResiduo()
    {
    }

    private TipoResiduo(
        Guid id,
        string codigo,
        string nome,
        CategoriaDeResiduo categoria,
        UnidadeDeMedida unidade,
        string? orientacoes)
    {
        Id = id;
        Codigo = codigo;
        Nome = nome;
        Categoria = categoria;
        Unidade = unidade;
        Orientacoes = orientacoes;
    }

    public Guid Id { get; private set; }

    public string Codigo { get; private set; } = string.Empty;

    public string Nome { get; private set; } = string.Empty;

    public CategoriaDeResiduo Categoria { get; private set; }

    public UnidadeDeMedida Unidade { get; private set; }

    public string? Orientacoes { get; private set; }

    public static TipoResiduo Criar(
        string codigo,
        string nome,
        CategoriaDeResiduo categoria,
        UnidadeDeMedida unidade,
        string? orientacoes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);

        var codigoNormalizado = codigo.Trim().ToLowerInvariant();
        if (codigoNormalizado.Length > CodigoMaxLength)
        {
            throw new ArgumentException(
                $"O código do tipo deve ter no máximo {CodigoMaxLength} caracteres.",
                nameof(codigo));
        }

        var nomeNormalizado = nome.Trim();
        if (nomeNormalizado.Length > NomeMaxLength)
        {
            throw new ArgumentException(
                $"O nome do tipo deve ter no máximo {NomeMaxLength} caracteres.",
                nameof(nome));
        }

        if (!Enum.IsDefined(categoria))
        {
            throw new ArgumentOutOfRangeException(nameof(categoria), categoria, "Categoria inválida.");
        }

        if (!Enum.IsDefined(unidade))
        {
            throw new ArgumentOutOfRangeException(nameof(unidade), unidade, "Unidade inválida.");
        }

        var orientacoesNormalizadas = string.IsNullOrWhiteSpace(orientacoes)
            ? null
            : orientacoes.Trim();

        if (OrientacoesExcedemLimite(orientacoesNormalizadas))
        {
            throw new ArgumentException(
                $"As orientações devem ter no máximo {OrientacoesMaxLength} caracteres.",
                nameof(orientacoes));
        }

        return new TipoResiduo(
            Guid.NewGuid(),
            codigoNormalizado,
            nomeNormalizado,
            categoria,
            unidade,
            orientacoesNormalizadas);
    }

    private static bool OrientacoesExcedemLimite(string? orientacoes)
    {
        return orientacoes is not null && orientacoes.Length > OrientacoesMaxLength;
    }
}
