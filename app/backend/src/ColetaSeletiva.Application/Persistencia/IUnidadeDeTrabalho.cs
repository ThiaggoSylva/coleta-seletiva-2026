namespace ColetaSeletiva.Application.Persistencia;

public interface IUnidadeDeTrabalho
{
    Task<ITransacao> IniciarAsync(CancellationToken cancellationToken = default);
}

public interface ITransacao : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
