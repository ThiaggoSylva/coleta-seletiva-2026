using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Domain.Entidades;
using FluentResults;

namespace ColetaSeletiva.Application.Acesso.Sessao;

public sealed class RenovarSessaoHandler
{
    private const int TamanhoMaximoDoValorDoRefresh = 256;

    private readonly IAutenticacaoDeContas _autenticacao;
    private readonly IGeradorDeTokenDeAcesso _geradorDeToken;
    private readonly IRepositorioDeRefreshTokens _refreshTokens;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly ConfiguracaoDeSessao _configuracao;
    private readonly TimeProvider _relogio;

    public RenovarSessaoHandler(
        IAutenticacaoDeContas autenticacao,
        IGeradorDeTokenDeAcesso geradorDeToken,
        IRepositorioDeRefreshTokens refreshTokens,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        ConfiguracaoDeSessao configuracao,
        TimeProvider relogio)
    {
        _autenticacao = autenticacao;
        _geradorDeToken = geradorDeToken;
        _refreshTokens = refreshTokens;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _configuracao = configuracao;
        _relogio = relogio;
    }

    public async Task<Result<SessaoEmitida>> ExecutarAsync(
        string? refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)
            || refreshToken.Length > TamanhoMaximoDoValorDoRefresh)
        {
            return Falha();
        }

        var hash = RefreshTokenOpaco.CalcularHash(refreshToken);
        var token = await _refreshTokens.ObterPorHashAsync(hash, cancellationToken);
        if (token is null)
        {
            return Falha();
        }

        var agora = _relogio.GetUtcNow();

        if (token.RevogadoEmUtc is not null)
        {
            return Falha();
        }

        if (token.ConsumidoEmUtc is not null)
        {
            await RevogarFamiliaAsync(token, agora, cancellationToken);
            return Falha();
        }

        if (token.ExpiraEmUtc <= agora)
        {
            return Falha();
        }

        var autenticacao = await _autenticacao.ObterPorUsuarioIdAsync(
            token.UsuarioId,
            cancellationToken);

        if (autenticacao.IsFailed || !autenticacao.Value.Ativa)
        {
            await RevogarFamiliaAsync(token, agora, cancellationToken);
            return Falha();
        }

        var conta = autenticacao.Value;
        var refreshOpaco = RefreshTokenOpaco.Gerar();

        var novoRefresh = new RefreshToken
        {
            UsuarioId = token.UsuarioId,
            FamiliaId = token.FamiliaId,
            Hash = refreshOpaco.Hash,
            CriadoEmUtc = agora,
            ExpiraEmUtc = agora.Add(_configuracao.ValidadeDoRefresh),
        };

        await using var transacao = await _unidadeDeTrabalho.IniciarAsync(cancellationToken);
        try
        {
            var consumido = await _refreshTokens.ConsumirAsync(
                token.Id,
                token.UsuarioId,
                agora,
                cancellationToken);

            if (!consumido)
            {
                await _refreshTokens.RevogarFamiliaAsync(
                    token.UsuarioId,
                    token.FamiliaId,
                    agora,
                    cancellationToken);

                await transacao.CommitAsync(cancellationToken);
                return Falha();
            }

            await _refreshTokens.AdicionarAsync(novoRefresh, cancellationToken);

            var tokenDeAcesso = _geradorDeToken.Gerar(conta, agora);

            await transacao.CommitAsync(cancellationToken);

            return Result.Ok(new SessaoEmitida(
                TokenDeAcesso: tokenDeAcesso.Valor,
                ExpiraEmUtc: tokenDeAcesso.ExpiraEmUtc,
                RefreshToken: refreshOpaco.Valor,
                RefreshExpiraEmUtc: novoRefresh.ExpiraEmUtc,
                Conta: conta));
        }
        catch
        {
            await transacao.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static Result<SessaoEmitida> Falha()
    {
        return Result.Fail<SessaoEmitida>(
            new ErroNaoAutenticado(MensagensDeSessao.SessaoInvalida));
    }

    private async Task RevogarFamiliaAsync(
        RefreshToken token,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        await using var transacao = await _unidadeDeTrabalho.IniciarAsync(cancellationToken);
        try
        {
            await _refreshTokens.RevogarFamiliaAsync(
                token.UsuarioId,
                token.FamiliaId,
                agora,
                cancellationToken);

            await transacao.CommitAsync(cancellationToken);
        }
        catch
        {
            await transacao.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
