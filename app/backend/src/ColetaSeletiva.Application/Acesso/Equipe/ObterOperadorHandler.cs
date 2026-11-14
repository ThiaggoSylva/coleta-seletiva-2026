using ColetaSeletiva.Application.Seguranca;
using FluentResults;

namespace ColetaSeletiva.Application.Acesso.Equipe;

public sealed class ObterOperadorHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IGerenciamentoDeEquipe _equipe;

    public ObterOperadorHandler(
        ITenantContext tenantContext,
        IGerenciamentoDeEquipe equipe)
    {
        _tenantContext = tenantContext;
        _equipe = equipe;
    }

    public async Task<Result<ContaDaEquipe>> ExecutarAsync(
        Guid operadorId,
        CancellationToken cancellationToken = default)
    {
        var escopo = EscopoDaEquipe.Obter(_tenantContext);
        if (escopo.IsFailed)
        {
            return Result.Fail<ContaDaEquipe>(escopo.Errors);
        }

        return await _equipe.ObterAsync(
            escopo.Value.OrganizacaoId,
            operadorId,
            cancellationToken);
    }
}
