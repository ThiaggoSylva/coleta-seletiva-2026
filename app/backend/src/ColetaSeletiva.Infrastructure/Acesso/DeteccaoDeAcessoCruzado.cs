using ColetaSeletiva.Application.Acesso.Equipe;
using ColetaSeletiva.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.Infrastructure.Acesso;

internal sealed class DeteccaoDeAcessoCruzado : IDeteccaoDeAcessoCruzado
{
    private readonly ColetaSeletivaDbContext _contexto;

    public DeteccaoDeAcessoCruzado(ColetaSeletivaDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<bool> ExisteEmOutraOrganizacaoAsync(
        Guid organizacaoId,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        return _contexto.Usuarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                usuario => usuario.Id == usuarioId
                    && usuario.OrganizacaoId != organizacaoId,
                cancellationToken);
    }
}
