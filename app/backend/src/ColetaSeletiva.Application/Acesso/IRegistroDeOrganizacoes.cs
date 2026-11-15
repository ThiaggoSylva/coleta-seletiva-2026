using ColetaSeletiva.Domain.Entidades;

namespace ColetaSeletiva.Application.Acesso;

public interface IRegistroDeOrganizacoes
{
    Task AdicionarAsync(Organizacao organizacao, CancellationToken cancellationToken = default);
}
