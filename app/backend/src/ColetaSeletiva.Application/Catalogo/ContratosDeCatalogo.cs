namespace ColetaSeletiva.Application.Catalogo;

public sealed record TipoResiduoDoCatalogo(
    Guid Id,
    string Codigo,
    string Nome,
    string Categoria,
    string Unidade,
    string? Orientacoes);

public sealed record MunicipioDoCatalogo(
    int Id,
    string Nome,
    string Uf);

public interface ICatalogoDeResiduos
{
    Task<IReadOnlyList<TipoResiduoDoCatalogo>> ListarTiposAsync(
        CancellationToken cancellationToken = default);
}

public interface ICatalogoDeMunicipios
{
    Task<IReadOnlyList<MunicipioDoCatalogo>> ListarAsync(
        string? uf,
        string? texto,
        int limite,
        CancellationToken cancellationToken = default);

    Task<bool> TodosExistemAsync(
        IReadOnlyCollection<int> municipioIds,
        CancellationToken cancellationToken = default);
}
