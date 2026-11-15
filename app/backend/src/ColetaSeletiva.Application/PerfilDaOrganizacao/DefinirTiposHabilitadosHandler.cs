using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;
using FluentValidation;

namespace ColetaSeletiva.Application.PerfilDaOrganizacao;

public sealed class DefinirTiposHabilitadosHandler : ManipuladorDoPerfilDaOrganizacao
{
    private readonly IValidator<DefinirTiposHabilitadosEntrada> _validador;
    private readonly IPerfilDaOrganizacao _perfil;

    public DefinirTiposHabilitadosHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IValidator<DefinirTiposHabilitadosEntrada> validador,
        IPerfilDaOrganizacao perfil)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _validador = validador;
        _perfil = perfil;
    }

    public async Task<Result<PerfilDaOrganizacaoResposta>> ExecutarAsync(
        DefinirTiposHabilitadosEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<PerfilDaOrganizacaoResposta>(
                validacao.Errors.Select(erro =>
                    new ErroDeValidacao(erro.PropertyName, erro.ErrorMessage)));
        }

        return await ExecutarComAuditoriaAsync(
            correlacaoId,
            AcoesAuditadas.TiposHabilitadosAtualizados,
            "tipos,aceitaRetiradaDomiciliar",
            (organizacaoId, _) => _perfil.DefinirTiposAsync(
                organizacaoId,
                entrada.Tipos,
                cancellationToken),
            cancellationToken);
    }
}
