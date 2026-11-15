using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Domain.Entidades;
using FluentResults;
using FluentValidation;

namespace ColetaSeletiva.Application.Acesso.Sessao;

public sealed class IniciarSessaoHandler
{
    private readonly IValidator<LoginEntrada> _validador;
    private readonly IAutenticacaoDeContas _autenticacao;
    private readonly IGeradorDeTokenDeAcesso _geradorDeToken;
    private readonly IRepositorioDeRefreshTokens _refreshTokens;
    private readonly IRegistroDeAuditoria _auditoria;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly ConfiguracaoDeSessao _configuracao;
    private readonly TimeProvider _relogio;

    public IniciarSessaoHandler(
        IValidator<LoginEntrada> validador,
        IAutenticacaoDeContas autenticacao,
        IGeradorDeTokenDeAcesso geradorDeToken,
        IRepositorioDeRefreshTokens refreshTokens,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        ConfiguracaoDeSessao configuracao,
        TimeProvider relogio)
    {
        _validador = validador;
        _autenticacao = autenticacao;
        _geradorDeToken = geradorDeToken;
        _refreshTokens = refreshTokens;
        _auditoria = auditoria;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _configuracao = configuracao;
        _relogio = relogio;
    }

    public async Task<Result<SessaoEmitida>> ExecutarAsync(
        LoginEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        if (correlacaoId == Guid.Empty)
        {
            throw new ArgumentException("A correlação do login não pode ser vazia.", nameof(correlacaoId));
        }

        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<SessaoEmitida>(
                validacao.Errors.Select(erro =>
                    new ErroDeValidacao(erro.PropertyName, erro.ErrorMessage)));
        }

        var identificador = entrada.Identificador.Trim();
        var autenticacao = await _autenticacao.AutenticarAsync(
            identificador,
            entrada.Senha,
            cancellationToken);

        if (autenticacao.IsFailed)
        {
            return Result.Fail<SessaoEmitida>(autenticacao.Errors);
        }

        var conta = autenticacao.Value;
        var agora = _relogio.GetUtcNow();
        var refreshOpaco = RefreshTokenOpaco.Gerar();

        var refresh = new RefreshToken
        {
            UsuarioId = conta.Id,
            FamiliaId = Guid.NewGuid(),
            Hash = refreshOpaco.Hash,
            CriadoEmUtc = agora,
            ExpiraEmUtc = agora.Add(_configuracao.ValidadeDoRefresh),
        };

        var token = _geradorDeToken.Gerar(conta, agora);

        await using var transacao = await _unidadeDeTrabalho.IniciarAsync(cancellationToken);
        try
        {
            await _refreshTokens.AdicionarAsync(refresh, cancellationToken);

            await _auditoria.RegistrarAsync(
                new EventoDeAuditoria(
                    OrganizacaoId: conta.OrganizacaoId,
                    AtorUsuarioId: conta.Id,
                    Acao: AcoesAuditadas.SessaoIniciada,
                    Entidade: EntidadesAuditadas.Usuario,
                    EntidadeId: conta.Id,
                    CorrelacaoId: correlacaoId,
                    OcorridoEmUtc: agora),
                cancellationToken);

            await transacao.CommitAsync(cancellationToken);

            return Result.Ok(new SessaoEmitida(
                TokenDeAcesso: token.Valor,
                ExpiraEmUtc: token.ExpiraEmUtc,
                RefreshToken: refreshOpaco.Valor,
                RefreshExpiraEmUtc: refresh.ExpiraEmUtc,
                Conta: conta));
        }
        catch
        {
            await transacao.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
