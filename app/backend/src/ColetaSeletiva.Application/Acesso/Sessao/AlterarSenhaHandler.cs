using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;
using FluentValidation;

namespace ColetaSeletiva.Application.Acesso.Sessao;

public sealed class AlterarSenhaHandler
{
    private readonly IValidator<AlterarSenhaEntrada> _validador;
    private readonly ITenantContext _tenantContext;
    private readonly IAlteracaoDeSenha _alteracao;
    private readonly IRepositorioDeRefreshTokens _refreshTokens;
    private readonly IRegistroDeAuditoria _auditoria;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly TimeProvider _relogio;

    public AlterarSenhaHandler(
        IValidator<AlterarSenhaEntrada> validador,
        ITenantContext tenantContext,
        IAlteracaoDeSenha alteracao,
        IRepositorioDeRefreshTokens refreshTokens,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio)
    {
        _validador = validador;
        _tenantContext = tenantContext;
        _alteracao = alteracao;
        _refreshTokens = refreshTokens;
        _auditoria = auditoria;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogio = relogio;
    }

    public async Task<Result> ExecutarAsync(
        AlterarSenhaEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        if (correlacaoId == Guid.Empty)
        {
            throw new ArgumentException(
                "A correlação da troca de senha não pode ser vazia.",
                nameof(correlacaoId));
        }

        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail(
                validacao.Errors.Select(erro =>
                    new ErroDeValidacao(erro.PropertyName, erro.ErrorMessage)));
        }

        if (!_tenantContext.Autenticado
            || _tenantContext.EscopoValido is false
            || _tenantContext.UsuarioId is not Guid usuarioId)
        {
            return Result.Fail(
                new ErroNaoAutenticado(MensagensDeSessao.SessaoInvalida));
        }

        var agora = _relogio.GetUtcNow();

        await using var transacao = await _unidadeDeTrabalho.IniciarAsync(cancellationToken);
        try
        {
            var alteracao = await _alteracao.AlterarAsync(
                usuarioId,
                entrada.SenhaAtual,
                entrada.NovaSenha,
                cancellationToken);

            if (alteracao.IsFailed)
            {
                await transacao.RollbackAsync(cancellationToken);
                return Result.Fail(alteracao.Errors);
            }

            await _refreshTokens.RevogarTodasAsync(usuarioId, agora, cancellationToken);

            await _auditoria.RegistrarAsync(
                new EventoDeAuditoria(
                    OrganizacaoId: _tenantContext.OrganizacaoId,
                    AtorUsuarioId: usuarioId,
                    Acao: AcoesAuditadas.SenhaAlterada,
                    Entidade: EntidadesAuditadas.Usuario,
                    EntidadeId: usuarioId,
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
