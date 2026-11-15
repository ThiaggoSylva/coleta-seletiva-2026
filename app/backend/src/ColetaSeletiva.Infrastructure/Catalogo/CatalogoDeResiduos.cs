using ColetaSeletiva.Application.Catalogo;
using ColetaSeletiva.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.Infrastructure.Catalogo;

internal sealed class CatalogoDeResiduos : ICatalogoDeResiduos
{
    private readonly ColetaSeletivaDbContext _dbContext;

    public CatalogoDeResiduos(ColetaSeletivaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TipoResiduoDoCatalogo>> ListarTiposAsync(
        CancellationToken cancellationToken = default)
    {
        var tipos = await _dbContext
            .TiposResiduo
            .AsNoTracking()
            .OrderBy(tipo => tipo.Nome)
            .ToListAsync(cancellationToken);

        return tipos
            .Select(tipo => new TipoResiduoDoCatalogo(
                Id: tipo.Id,
                Codigo: tipo.Codigo,
                Nome: tipo.Nome,
                Categoria: tipo.Categoria.ToString(),
                Unidade: tipo.Unidade.ToString(),
                Orientacoes: tipo.Orientacoes))
            .ToList();
    }
}
