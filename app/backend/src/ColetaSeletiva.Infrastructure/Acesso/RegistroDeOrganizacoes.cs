using ColetaSeletiva.Application.Acesso;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure.Persistencia;

namespace ColetaSeletiva.Infrastructure.Acesso;

internal sealed class RegistroDeOrganizacoes : IRegistroDeOrganizacoes
{
    private readonly ColetaSeletivaDbContext _contexto;

    public RegistroDeOrganizacoes(ColetaSeletivaDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task AdicionarAsync(
        Organizacao organizacao,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizacao);

        _contexto.Organizacoes.Add(organizacao);
        await _contexto.SaveChangesAsync(cancellationToken);
    }
}
