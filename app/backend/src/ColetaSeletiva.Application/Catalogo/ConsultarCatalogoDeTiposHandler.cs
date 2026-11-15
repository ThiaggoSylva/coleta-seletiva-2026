namespace ColetaSeletiva.Application.Catalogo;

public sealed class ConsultarCatalogoDeTiposHandler
{
    private readonly ICatalogoDeResiduos _catalogo;

    public ConsultarCatalogoDeTiposHandler(ICatalogoDeResiduos catalogo)
    {
        _catalogo = catalogo;
    }

    public Task<IReadOnlyList<TipoResiduoDoCatalogo>> ExecutarAsync(
        CancellationToken cancellationToken = default)
    {
        return _catalogo.ListarTiposAsync(cancellationToken);
    }
}
