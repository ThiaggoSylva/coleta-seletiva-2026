using ColetaSeletiva.Application.Seguranca;
using FluentResults;

namespace ColetaSeletiva.Application.Acesso.Equipe;

public sealed class ListarOperadoresHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IGerenciamentoDeEquipe _equipe;

    public ListarOperadoresHandler(
        ITenantContext tenantContext,
        IGerenciamentoDeEquipe equipe)
    {
        _tenantContext = tenantContext;
        _equipe = equipe;
    }

    public async Task<Result<IReadOnlyList<ContaDaEquipe>>> ExecutarAsync(
        CancellationToken cancellationToken = default)
    {
        var escopo = EscopoDaEquipe.Obter(_tenantContext);
        if (escopo.IsFailed)
        {
            return Result.Fail<IReadOnlyList<ContaDaEquipe>>(escopo.Errors);
        }

        return await _equipe.ListarAsync(
            escopo.Value.OrganizacaoId,
            cancellationToken);
    }
}
