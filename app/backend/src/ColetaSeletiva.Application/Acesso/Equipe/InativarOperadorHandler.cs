using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;

namespace ColetaSeletiva.Application.Acesso.Equipe;

public sealed class InativarOperadorHandler
{
    private const string CamposRegistrados = "ativo";

    private readonly ITenantContext _tenantContext;
    private readonly IGerenciamentoDeEquipe _equipe;
    private readonly IDeteccaoDeAcessoCruzado _deteccao;
    private readonly IRegistroDeAuditoria _auditoria;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly TimeProvider _relogio;

    public InativarOperadorHandler(
        ITenantContext tenantContext,
        IGerenciamentoDeEquipe equipe,
        IDeteccaoDeAcessoCruzado deteccao,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio)
    {
        _tenantContext = tenantContext;
        _equipe = equipe;
        _deteccao = deteccao;
        _auditoria = auditoria;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogio = relogio;
    }

    public async Task<Result<ContaDaEquipe>> ExecutarAsync(
        Guid operadorId,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        if (correlacaoId == Guid.Empty)
        {
            throw new ArgumentException(
                "A correlação da inativação não pode ser vazia.",
                nameof(correlacaoId));
        }

        var escopo = EscopoDaEquipe.Obter(_tenantContext);
        if (escopo.IsFailed)
        {
            return Result.Fail<ContaDaEquipe>(escopo.Errors);
        }

        await using var transacao = await _unidadeDeTrabalho.IniciarAsync(cancellationToken);
        try
        {
            var inativacao = await _equipe.InativarAsync(
                escopo.Value.OrganizacaoId,
                operadorId,
                cancellationToken);

            if (inativacao.IsFailed)
            {
                if (inativacao.Errors.OfType<ErroNaoEncontrado>().Any()
                    && await AuditoriaDeAcessoCruzado.RegistrarSeAplicavelAsync(
                        _deteccao,
                        _auditoria,
                        _relogio,
                        escopo.Value.OrganizacaoId,
                        escopo.Value.AtorUsuarioId,
                        operadorId,
                        correlacaoId,
                        cancellationToken))
                {
                    await transacao.CommitAsync(cancellationToken);
                    return Result.Fail<ContaDaEquipe>(inativacao.Errors);
                }

                await transacao.RollbackAsync(cancellationToken);
                return Result.Fail<ContaDaEquipe>(inativacao.Errors);
            }

            await _auditoria.RegistrarAsync(
                new EventoDeAuditoria(
                    OrganizacaoId: escopo.Value.OrganizacaoId,
                    AtorUsuarioId: escopo.Value.AtorUsuarioId,
                    Acao: AcoesAuditadas.OperadorInativado,
                    Entidade: EntidadesAuditadas.Usuario,
                    EntidadeId: inativacao.Value.Id,
                    CorrelacaoId: correlacaoId,
                    OcorridoEmUtc: _relogio.GetUtcNow(),
                    CamposAlterados: CamposRegistrados),
                cancellationToken);

            await transacao.CommitAsync(cancellationToken);

            return inativacao;
        }
        catch
        {
            await transacao.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
