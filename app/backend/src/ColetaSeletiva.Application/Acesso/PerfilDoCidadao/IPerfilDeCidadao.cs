using ColetaSeletiva.Application.Erros;
using FluentResults;

namespace ColetaSeletiva.Application.Acesso.PerfilDoCidadao;

public sealed record PerfilDoCidadao(
    Guid Id,
    string Nome,
    string Email,
    string? Telefone);

public sealed record AtualizarPerfilDoCidadaoEntrada(
    string Nome,
    string Email,
    string? Telefone);

public interface IPerfilDeCidadao
{
    Task<Result<PerfilDoCidadao>> ObterAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);

    Task<Result<PerfilDoCidadao>> AtualizarAsync(
        Guid usuarioId,
        AtualizarPerfilDoCidadaoEntrada entrada,
        CancellationToken cancellationToken = default);
}
