namespace ColetaSeletiva.Application.Roadmap;

/// <summary>
/// Serviço de consulta do catálogo de placeholders, usado pela camada de API (DT-38).
/// Não acessa persistência e não produz efeito colateral.
/// </summary>
public sealed class ConsultarPlaceholdersHandler
{
    private readonly ICatalogoDePlaceholders _catalogo;

    public ConsultarPlaceholdersHandler(ICatalogoDePlaceholders catalogo)
    {
        _catalogo = catalogo;
    }

    public IReadOnlyList<ModuloEmBreve> Listar() => _catalogo.Listar();

    public ModuloEmBreve? ObterPorId(string id) => _catalogo.ObterPorId(id);
}
