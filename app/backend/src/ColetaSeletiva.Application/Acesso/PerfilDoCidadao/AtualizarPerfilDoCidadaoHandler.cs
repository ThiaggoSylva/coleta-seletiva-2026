using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;
using FluentValidation;

namespace ColetaSeletiva.Application.Acesso.PerfilDoCidadao;

public sealed class AtualizarPerfilDoCidadaoHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IValidator<AtualizarPerfilDoCidadaoEntrada> _validador;
    private readonly IPerfilDeCidadao _perfis;

    public AtualizarPerfilDoCidadaoHandler(
        ITenantContext tenantContext,
        IValidator<AtualizarPerfilDoCidadaoEntrada> validador,
        IPerfilDeCidadao perfis)
    {
        _tenantContext = tenantContext;
        _validador = validador;
        _perfis = perfis;
    }

    public async Task<Result<PerfilDoCidadao>> ExecutarAsync(
        AtualizarPerfilDoCidadaoEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var usuarioId = ObterUsuarioId();
        if (usuarioId.IsFailed)
        {
            return Result.Fail<PerfilDoCidadao>(usuarioId.Errors);
        }

        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<PerfilDoCidadao>(
                validacao.Errors.Select(erro =>
                    new ErroDeValidacao(erro.PropertyName, erro.ErrorMessage)));
        }

        return await _perfis.AtualizarAsync(usuarioId.Value, entrada, cancellationToken);
    }

    private Result<Guid> ObterUsuarioId()
    {
        if (!_tenantContext.Autenticado)
        {
            return Result.Fail<Guid>(
                new ErroNaoAutenticado("É necessário autenticar para alterar o perfil."));
        }

        if (!_tenantContext.EhCidadao)
        {
            return Result.Fail<Guid>(
                new ErroDeAcessoNegado("O perfil é exclusivo do cidadão."));
        }

        if (!_tenantContext.EscopoValido || _tenantContext.UsuarioId is not Guid usuarioId)
        {
            return Result.Fail<Guid>(
                new ErroNaoAutenticado("A sessão não possui um cidadão válido."));
        }

        return Result.Ok(usuarioId);
    }
}
