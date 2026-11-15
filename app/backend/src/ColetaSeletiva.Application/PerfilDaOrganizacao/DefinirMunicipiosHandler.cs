using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;
using FluentValidation;

namespace ColetaSeletiva.Application.PerfilDaOrganizacao;

public sealed class DefinirMunicipiosHandler : ManipuladorDoPerfilDaOrganizacao
{
    private readonly IValidator<DefinirMunicipiosEntrada> _validador;
    private readonly IPerfilDaOrganizacao _perfil;

    public DefinirMunicipiosHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IValidator<DefinirMunicipiosEntrada> validador,
        IPerfilDaOrganizacao perfil)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _validador = validador;
        _perfil = perfil;
    }

    public async Task<Result<PerfilDaOrganizacaoResposta>> ExecutarAsync(
        DefinirMunicipiosEntrada entrada,
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
            AcoesAuditadas.MunicipiosAtualizados,
            "municipios",
            (organizacaoId, _) => _perfil.DefinirMunicipiosAsync(
                organizacaoId,
                entrada.MunicipioIds,
                cancellationToken),
            cancellationToken);
    }
}
