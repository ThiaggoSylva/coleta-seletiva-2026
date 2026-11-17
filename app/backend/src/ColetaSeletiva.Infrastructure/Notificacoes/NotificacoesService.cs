using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Retiradas;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure.Persistencia;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.Infrastructure.Notificacoes;

internal sealed class NotificacoesService : INotificacoes
{
    private const int LimiteDeItens = 50;

    private readonly ColetaSeletivaDbContext _dbContext;
    private readonly TimeProvider _relogio;

    public NotificacoesService(ColetaSeletivaDbContext dbContext, TimeProvider relogio)
    {
        _dbContext = dbContext;
        _relogio = relogio;
    }

    public async Task<ListaDeNotificacoesResposta> ListarAsync(
        Guid usuarioId,
        DateTimeOffset? desde,
        bool apenasNaoLidas,
        CancellationToken cancellationToken = default)
    {
        var consulta = _dbContext
            .Notificacoes
            .AsNoTracking()
            .Where(notificacao => notificacao.UsuarioId == usuarioId);

        if (desde is DateTimeOffset limite)
        {
            consulta = consulta.Where(notificacao => notificacao.CriadaEmUtc >= limite);
        }

        if (apenasNaoLidas)
        {
            consulta = consulta.Where(notificacao => !notificacao.Lida);
        }

        var itens = await consulta
            .OrderByDescending(notificacao => notificacao.CriadaEmUtc)
            .Take(LimiteDeItens)
            .ToListAsync(cancellationToken);

        var naoLidas = await _dbContext
            .Notificacoes
            .AsNoTracking()
            .CountAsync(
                notificacao => notificacao.UsuarioId == usuarioId && !notificacao.Lida,
                cancellationToken);

        return new ListaDeNotificacoesResposta(
            itens.Select(Mapear).ToList(),
            naoLidas);
    }

    public async Task<Result<NotificacaoResposta>> MarcarComoLidaAsync(
        Guid usuarioId,
        Guid notificacaoId,
        CancellationToken cancellationToken = default)
    {
        var notificacao = await _dbContext
            .Notificacoes
            .FirstOrDefaultAsync(
                item => item.Id == notificacaoId && item.UsuarioId == usuarioId,
                cancellationToken);

        if (notificacao is null)
        {
            return Result.Fail<NotificacaoResposta>(
                new ErroNaoEncontrado("Notificação não encontrada."));
        }

        notificacao.MarcarLida(_relogio.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(Mapear(notificacao));
    }

    public async Task<Result<int>> MarcarTodasComoLidasAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var notificacoes = await _dbContext
            .Notificacoes
            .Where(notificacao => notificacao.UsuarioId == usuarioId && !notificacao.Lida)
            .ToListAsync(cancellationToken);

        if (notificacoes.Count == 0)
        {
            return Result.Ok(0);
        }

        var agora = _relogio.GetUtcNow();
        foreach (var notificacao in notificacoes)
        {
            notificacao.MarcarLida(agora);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(notificacoes.Count);
    }

    private static NotificacaoResposta Mapear(Notificacao notificacao)
    {
        return new NotificacaoResposta(
            notificacao.Id,
            notificacao.Tipo.ToString(),
            notificacao.ReferenciaId,
            notificacao.Titulo,
            notificacao.Mensagem,
            notificacao.Lida,
            notificacao.LidaEmUtc,
            notificacao.CriadaEmUtc);
    }
}
