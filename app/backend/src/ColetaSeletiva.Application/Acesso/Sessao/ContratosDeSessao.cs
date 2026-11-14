using ColetaSeletiva.Domain.Entidades;
using FluentResults;

namespace ColetaSeletiva.Application.Acesso.Sessao;

public sealed record ContaAutenticada(
    Guid Id,
    string Nome,
    string Email,
    string Papel,
    Guid? OrganizacaoId,
    bool TrocaSenhaPendente,
    bool Ativa);

public sealed record TokenDeAcesso(string Valor, DateTimeOffset ExpiraEmUtc);

public sealed record SessaoEmitida(
    string TokenDeAcesso,
    DateTimeOffset ExpiraEmUtc,
    string RefreshToken,
    DateTimeOffset RefreshExpiraEmUtc,
    ContaAutenticada Conta);

public interface IAutenticacaoDeContas
{
    Task<Result<ContaAutenticada>> AutenticarAsync(
        string identificador,
        string senha,
        CancellationToken cancellationToken = default);

    Task<Result<ContaAutenticada>> ObterPorUsuarioIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);
}

public interface IGeradorDeTokenDeAcesso
{
    TokenDeAcesso Gerar(ContaAutenticada conta, DateTimeOffset agora);
}

public interface IRepositorioDeRefreshTokens
{
    Task AdicionarAsync(RefreshToken token, CancellationToken cancellationToken = default);

    Task<RefreshToken?> ObterPorHashAsync(
        string hash,
        CancellationToken cancellationToken = default);

    Task<bool> ConsumirAsync(
        Guid id,
        Guid usuarioId,
        DateTimeOffset consumidoEmUtc,
        CancellationToken cancellationToken = default);

    Task RevogarFamiliaAsync(
        Guid usuarioId,
        Guid familiaId,
        DateTimeOffset revogadoEmUtc,
        CancellationToken cancellationToken = default);

    Task RevogarTodasAsync(
        Guid usuarioId,
        DateTimeOffset revogadoEmUtc,
        CancellationToken cancellationToken = default);
}
