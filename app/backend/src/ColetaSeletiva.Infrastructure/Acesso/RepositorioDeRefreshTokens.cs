using ColetaSeletiva.Application.Acesso.Sessao;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.Infrastructure.Acesso;

internal sealed class RepositorioDeRefreshTokens : IRepositorioDeRefreshTokens
{
    private readonly ColetaSeletivaDbContext _contexto;

    public RepositorioDeRefreshTokens(ColetaSeletivaDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task AdicionarAsync(
        RefreshToken token,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        _contexto.RefreshTokens.Add(token);
        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshToken?> ObterPorHashAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        return await _contexto.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.Hash == hash, cancellationToken);
    }

    public async Task<bool> ConsumirAsync(
        Guid id,
        Guid usuarioId,
        DateTimeOffset consumidoEmUtc,
        CancellationToken cancellationToken = default)
    {
        var linhasAfetadas = await _contexto.RefreshTokens
            .Where(token => token.Id == id
                && token.UsuarioId == usuarioId
                && token.ConsumidoEmUtc == null
                && token.RevogadoEmUtc == null)
            .ExecuteUpdateAsync(
                atualizacao => atualizacao.SetProperty(
                    token => token.ConsumidoEmUtc,
                    consumidoEmUtc),
                cancellationToken);

        return linhasAfetadas == 1;
    }

    public async Task RevogarFamiliaAsync(
        Guid usuarioId,
        Guid familiaId,
        DateTimeOffset revogadoEmUtc,
        CancellationToken cancellationToken = default)
    {
        await _contexto.RefreshTokens
            .Where(token => token.UsuarioId == usuarioId
                && token.FamiliaId == familiaId
                && token.RevogadoEmUtc == null)
            .ExecuteUpdateAsync(
                atualizacao => atualizacao.SetProperty(
                    token => token.RevogadoEmUtc,
                    revogadoEmUtc),
                cancellationToken);
    }
}
