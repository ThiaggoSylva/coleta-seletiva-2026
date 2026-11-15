using FluentResults;

namespace ColetaSeletiva.Application.Acesso.Sessao;

public interface IAlteracaoDeSenha
{
    Task<Result> AlterarAsync(
        Guid usuarioId,
        string senhaAtual,
        string novaSenha,
        CancellationToken cancellationToken = default);
}
