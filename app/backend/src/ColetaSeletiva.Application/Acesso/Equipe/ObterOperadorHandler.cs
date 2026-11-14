using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;

namespace ColetaSeletiva.Application.Acesso.Equipe;

public sealed class ObterOperadorHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IGerenciamentoDeEquipe _equipe;
    private readonly IDeteccaoDeAcessoCruzado _deteccao;
    private readonly IRegistroDeAuditoria _auditoria;
    private readonly TimeProvider _relogio;

    public ObterOperadorHandler(
        ITenantContext tenantContext,
        IGerenciamentoDeEquipe equipe,
        IDeteccaoDeAcessoCruzado deteccao,
        IRegistroDeAuditoria auditoria,
        TimeProvider relogio)
    {
        _tenantContext = tenantContext;
        _equipe = equipe;
        _deteccao = deteccao;
        _auditoria = auditoria;
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
                "A correlação da consulta não pode ser vazia.",
                nameof(correlacaoId));
        }

        var escopo = EscopoDaEquipe.Obter(_tenantContext);
        if (escopo.IsFailed)
        {
            return Result.Fail<ContaDaEquipe>(escopo.Errors);
        }

        var resultado = await _equipe.ObterAsync(
            escopo.Value.OrganizacaoId,
            operadorId,
            cancellationToken);

        if (resultado.IsFailed
            && resultado.Errors.OfType<ErroNaoEncontrado>().Any())
        {
            await AuditoriaDeAcessoCruzado.RegistrarSeAplicavelAsync(
                _deteccao,
                _auditoria,
                _relogio,
                escopo.Value.OrganizacaoId,
                escopo.Value.AtorUsuarioId,
                operadorId,
                correlacaoId,
                cancellationToken);
        }

        return resultado;
    }
}
