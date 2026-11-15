using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore.Storage;

namespace ColetaSeletiva.Infrastructure.Persistencia;

internal sealed class UnidadeDeTrabalho : IUnidadeDeTrabalho
{
    private readonly ColetaSeletivaDbContext _contexto;

    public UnidadeDeTrabalho(ColetaSeletivaDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<ITransacao> IniciarAsync(CancellationToken cancellationToken = default)
    {
        var transacao = await _contexto.Database.BeginTransactionAsync(cancellationToken);
        return new Transacao(transacao);
    }

    private sealed class Transacao : ITransacao
    {
        private readonly IDbContextTransaction _transacao;
        private bool _finalizada;

        public Transacao(IDbContextTransaction transacao)
        {
            _transacao = transacao;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_finalizada)
            {
                return;
            }

            await _transacao.CommitAsync(cancellationToken);
            _finalizada = true;
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_finalizada)
            {
                return;
            }

            await _transacao.RollbackAsync(cancellationToken);
            _finalizada = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_finalizada)
            {
                await _transacao.RollbackAsync();
            }

            await _transacao.DisposeAsync();
        }
    }
}
