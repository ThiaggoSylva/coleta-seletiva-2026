using FluentResults;

namespace ColetaSeletiva.Application.Acesso;

public sealed record ContaNova(
    string Nome,
    string Usuario,
    string Email,
    string Senha,
    Guid? OrganizacaoId,
    bool TrocaSenhaPendente);

public interface ICadastroDeContas
{
    Task<Result<Guid>> CriarAsync(
        ContaNova conta,
        string papel,
        CancellationToken cancellationToken = default);
}
