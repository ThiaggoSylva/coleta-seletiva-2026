using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;
using FluentValidation;

namespace ColetaSeletiva.Application.PerfilDaOrganizacao;

public sealed class AtualizarPerfilDaOrganizacaoHandler : ManipuladorDoPerfilDaOrganizacao
{
    private const string CamposRegistrados =
        "nome,tipo,contatoEmail,contatoTelefone,fuso,limiteSolicitacoesAbertas";

    private readonly IValidator<AtualizarPerfilDaOrganizacaoEntrada> _validador;
    private readonly IPerfilDaOrganizacao _perfil;

    public AtualizarPerfilDaOrganizacaoHandler(
        ITenantContext tenantContext,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        IValidator<AtualizarPerfilDaOrganizacaoEntrada> validador,
        IPerfilDaOrganizacao perfil)
        : base(tenantContext, auditoria, unidadeDeTrabalho, relogio)
    {
        _validador = validador;
        _perfil = perfil;
    }

    public async Task<Result<PerfilDaOrganizacaoResposta>> ExecutarAsync(
        AtualizarPerfilDaOrganizacaoEntrada entrada,
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
            AcoesAuditadas.OrganizacaoAtualizada,
            CamposRegistrados,
            (organizacaoId, _) => _perfil.AtualizarAsync(organizacaoId, entrada, cancellationToken),
            cancellationToken);
    }
}
