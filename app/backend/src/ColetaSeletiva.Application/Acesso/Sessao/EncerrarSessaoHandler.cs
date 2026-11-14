using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Domain.Entidades;
using FluentResults;

namespace ColetaSeletiva.Application.Acesso.Sessao;

public sealed class EncerrarSessaoHandler
{
    private const int TamanhoMaximoDoValorDoRefresh = 256;

    private readonly IAutenticacaoDeContas _autenticacao;
    private readonly IRepositorioDeRefreshTokens _refreshTokens;
    private readonly IRegistroDeAuditoria _auditoria;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly TimeProvider _relogio;

    public EncerrarSessaoHandler(
        IAutenticacaoDeContas autenticacao,
        IRepositorioDeRefreshTokens refreshTokens,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio)
    {
        _autenticacao = autenticacao;
        _refreshTokens = refreshTokens;
        _auditoria = auditoria;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogio = relogio;
    }

    public async Task<Result> ExecutarAsync(
        string? refreshToken,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        if (correlacaoId == Guid.Empty)
        {
            throw new ArgumentException("A correlação do logout não pode ser vazia.", nameof(correlacaoId));
        }

        if (string.IsNullOrWhiteSpace(refreshToken)
            || refreshToken.Length > TamanhoMaximoDoValorDoRefresh)
        {
            return Result.Ok();
        }

        var hash = RefreshTokenOpaco.CalcularHash(refreshToken);
        var token = await _refreshTokens.ObterPorHashAsync(hash, cancellationToken);
        if (token is null || token.RevogadoEmUtc is not null)
        {
            return Result.Ok();
        }

        var agora = _relogio.GetUtcNow();
        var autenticacao = await _autenticacao.ObterPorUsuarioIdAsync(
            token.UsuarioId,
            cancellationToken);

        await using var transacao = await _unidadeDeTrabalho.IniciarAsync(cancellationToken);
        try
        {
            await _refreshTokens.RevogarFamiliaAsync(
                token.UsuarioId,
                token.FamiliaId,
                agora,
                cancellationToken);

            await _auditoria.RegistrarAsync(
                new EventoDeAuditoria(
                    OrganizacaoId: autenticacao.IsSuccess
                        ? autenticacao.Value.OrganizacaoId
                        : null,
                    AtorUsuarioId: token.UsuarioId,
                    Acao: AcoesAuditadas.SessaoEncerrada,
                    Entidade: EntidadesAuditadas.Usuario,
                    EntidadeId: token.UsuarioId,
                    CorrelacaoId: correlacaoId,
                    OcorridoEmUtc: agora),
                cancellationToken);

            await transacao.CommitAsync(cancellationToken);
            return Result.Ok();
        }
        catch
        {
            await transacao.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
