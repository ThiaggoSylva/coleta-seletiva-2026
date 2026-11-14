using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;
using FluentValidation;

namespace ColetaSeletiva.Application.Acesso.Equipe;

public sealed class EditarOperadorHandler
{
    private const string CamposRegistrados = "nome,usuario,email";

    private readonly ITenantContext _tenantContext;
    private readonly IValidator<EditarOperadorEntrada> _validador;
    private readonly IGerenciamentoDeEquipe _equipe;
    private readonly IRegistroDeAuditoria _auditoria;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly TimeProvider _relogio;

    public EditarOperadorHandler(
        ITenantContext tenantContext,
        IValidator<EditarOperadorEntrada> validador,
        IGerenciamentoDeEquipe equipe,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio)
    {
        _tenantContext = tenantContext;
        _validador = validador;
        _equipe = equipe;
        _auditoria = auditoria;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogio = relogio;
    }

    public async Task<Result<ContaDaEquipe>> ExecutarAsync(
        Guid operadorId,
        EditarOperadorEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        if (correlacaoId == Guid.Empty)
        {
            throw new ArgumentException(
                "A correlação da edição não pode ser vazia.",
                nameof(correlacaoId));
        }

        var escopo = EscopoDaEquipe.Obter(_tenantContext);
        if (escopo.IsFailed)
        {
            return Result.Fail<ContaDaEquipe>(escopo.Errors);
        }

        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<ContaDaEquipe>(
                validacao.Errors.Select(erro =>
                    new ErroDeValidacao(erro.PropertyName, erro.ErrorMessage)));
        }

        await using var transacao = await _unidadeDeTrabalho.IniciarAsync(cancellationToken);
        try
        {
            var atualizacao = await _equipe.AtualizarAsync(
                escopo.Value.OrganizacaoId,
                operadorId,
                entrada,
                cancellationToken);

            if (atualizacao.IsFailed)
            {
                await transacao.RollbackAsync(cancellationToken);
                return Result.Fail<ContaDaEquipe>(atualizacao.Errors);
            }

            await _auditoria.RegistrarAsync(
                new EventoDeAuditoria(
                    OrganizacaoId: escopo.Value.OrganizacaoId,
                    AtorUsuarioId: escopo.Value.AtorUsuarioId,
                    Acao: AcoesAuditadas.OperadorEditado,
                    Entidade: EntidadesAuditadas.Usuario,
                    EntidadeId: atualizacao.Value.Id,
                    CorrelacaoId: correlacaoId,
                    OcorridoEmUtc: _relogio.GetUtcNow(),
                    CamposAlterados: CamposRegistrados),
                cancellationToken);

            await transacao.CommitAsync(cancellationToken);

            return atualizacao;
        }
        catch
        {
            await transacao.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
