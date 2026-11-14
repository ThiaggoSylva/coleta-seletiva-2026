using FluentResults;

namespace ColetaSeletiva.Application.Acesso.Equipe;

public sealed record ContaDaEquipe(
    Guid Id,
    string Nome,
    string Usuario,
    string Email,
    bool Ativo,
    bool TrocaSenhaPendente);

public sealed record CriarOperadorEntrada(
    string Nome,
    string Usuario,
    string Email,
    string Senha);

public sealed record EditarOperadorEntrada(
    string Nome,
    string Usuario,
    string Email);

public interface IGerenciamentoDeEquipe
{
    Task<Result<ContaDaEquipe>> CriarAsync(
        Guid organizacaoId,
        CriarOperadorEntrada entrada,
        CancellationToken cancellationToken = default);

    Task<Result<ContaDaEquipe>> ObterAsync(
        Guid organizacaoId,
        Guid operadorId,
        CancellationToken cancellationToken = default);

    Task<Result<ContaDaEquipe>> AtualizarAsync(
        Guid organizacaoId,
        Guid operadorId,
        EditarOperadorEntrada entrada,
        CancellationToken cancellationToken = default);
}
