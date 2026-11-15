namespace ColetaSeletiva.Domain.Catalogo;

/// <summary>
/// Unidade fixa de um tipo de resíduo. Não há conversão entre unidades na v1 (RN-06).
/// </summary>
public enum UnidadeDeMedida
{
    Quilograma,
    Litro,
    Unidade,
}

/// <summary>
/// Categoria de agrupamento do catálogo de resíduos, usada apenas para exibição.
/// </summary>
public enum CategoriaDeResiduo
{
    Papel,
    Plastico,
    Vidro,
    Metal,
    Organico,
    Eletronico,
    Oleo,
    Pilhas,
    Outros,
}
