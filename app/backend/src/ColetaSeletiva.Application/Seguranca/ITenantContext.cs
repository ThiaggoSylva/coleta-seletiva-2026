namespace ColetaSeletiva.Application.Seguranca;

public interface ITenantContext
{
    bool Autenticado { get; }

    bool EscopoValido { get; }

    bool EhCidadao { get; }

    Guid? UsuarioId { get; }

    Guid? OrganizacaoId { get; }
}
