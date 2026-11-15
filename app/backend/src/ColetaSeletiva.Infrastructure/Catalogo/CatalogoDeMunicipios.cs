using ColetaSeletiva.Application.Catalogo;
using ColetaSeletiva.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.Infrastructure.Catalogo;

internal sealed class CatalogoDeMunicipios : ICatalogoDeMunicipios
{
    private readonly ColetaSeletivaDbContext _dbContext;

    public CatalogoDeMunicipios(ColetaSeletivaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MunicipioDoCatalogo>> ListarAsync(
        string? uf,
        string? texto,
        int limite,
        CancellationToken cancellationToken = default)
    {
        var consulta = _dbContext.Municipios.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(uf))
        {
            consulta = consulta.Where(municipio => municipio.Uf == uf);
        }

        if (!string.IsNullOrWhiteSpace(texto))
        {
            consulta = consulta.Where(municipio => municipio.Nome.Contains(texto));
        }

        return await consulta
            .OrderBy(municipio => municipio.Nome)
            .ThenBy(municipio => municipio.Id)
            .Take(limite)
            .Select(municipio => new MunicipioDoCatalogo(
                municipio.Id,
                municipio.Nome,
                municipio.Uf))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TodosExistemAsync(
        IReadOnlyCollection<int> municipioIds,
        CancellationToken cancellationToken = default)
    {
        if (municipioIds.Count == 0)
        {
            return true;
        }

        var existentes = await _dbContext
            .Municipios
            .AsNoTracking()
            .CountAsync(municipio => municipioIds.Contains(municipio.Id), cancellationToken);

        return existentes == municipioIds.Distinct().Count();
    }
}
