using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;

namespace ColetaSeletiva.Application.Acesso.Sessao;

public sealed class ObterContaAtualHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IAutenticacaoDeContas _autenticacao;

    public ObterContaAtualHandler(
        ITenantContext tenantContext,
        IAutenticacaoDeContas autenticacao)
    {
        _tenantContext = tenantContext;
        _autenticacao = autenticacao;
    }

    public async Task<Result<ContaAutenticada>> ExecutarAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.Autenticado
            || _tenantContext.UsuarioId is not Guid usuarioId)
        {
            return Result.Fail<ContaAutenticada>(
                new ErroNaoAutenticado(MensagensDeSessao.SessaoInvalida));
        }

        var resultado = await _autenticacao.ObterPorUsuarioIdAsync(
            usuarioId,
            cancellationToken);

        if (resultado.IsFailed || !resultado.Value.Ativa)
        {
            return Result.Fail<ContaAutenticada>(
                new ErroNaoAutenticado(MensagensDeSessao.SessaoInvalida));
        }

        return resultado;
    }
}
