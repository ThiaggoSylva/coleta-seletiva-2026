using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;

namespace ColetaSeletiva.Application.PerfilDaOrganizacao;

/// <summary>
/// Concentra escopo de organização, transação e auditoria dos handlers de perfil (RF-06).
/// </summary>
public abstract class ManipuladorDoPerfilDaOrganizacao
{
    private readonly ITenantContext _tenantContext;
    private readonly IRegistroDeAuditoria _auditoria;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly TimeProvider _relogio;

    protected ManipuladorDoPerfilDaOrganizacao(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio)
    {
        _tenantContext = tenantContext;
        _auditoria = auditoria;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogio = relogio;
    }

    protected Result<EscopoDaOrganizacao> ObterEscopo()
    {
        return EscopoDaOrganizacaoHelper.Obter(_tenantContext);
    }

    protected async Task<Result<T>> ExecutarComAuditoriaAsync<T>(
        Guid correlacaoId,
        string acao,
        string? camposAlterados,
        Func<Guid, Guid, Task<Result<T>>> operacao,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operacao);

        if (correlacaoId == Guid.Empty)
        {
            throw new ArgumentException(
                "A correlação da operação não pode ser vazia.",
                nameof(correlacaoId));
        }

        var escopo = ObterEscopo();
        if (escopo.IsFailed)
        {
            return Result.Fail<T>(escopo.Errors);
        }

        await using var transacao = await _unidadeDeTrabalho.IniciarAsync(cancellationToken);

        try
        {
            var resultado = await operacao(
                escopo.Value.OrganizacaoId,
                escopo.Value.AtorUsuarioId);

            if (resultado.IsFailed)
            {
                await transacao.RollbackAsync(cancellationToken);
                return resultado;
            }

            await _auditoria.RegistrarAsync(
                new EventoDeAuditoria(
                    OrganizacaoId: escopo.Value.OrganizacaoId,
                    AtorUsuarioId: escopo.Value.AtorUsuarioId,
                    Acao: acao,
                    Entidade: EntidadesAuditadas.Organizacao,
                    EntidadeId: escopo.Value.OrganizacaoId,
                    CorrelacaoId: correlacaoId,
                    OcorridoEmUtc: _relogio.GetUtcNow(),
                    CamposAlterados: camposAlterados),
                cancellationToken);

            await transacao.CommitAsync(cancellationToken);

            return resultado;
        }
        catch
        {
            await transacao.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
