using ColetaSeletiva.Application.Seguranca;

namespace ColetaSeletiva.Infrastructure.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public bool Autenticado { get; private set; }

    public bool EscopoValido { get; private set; }

    public bool EhCidadao { get; private set; }

    public Guid? UsuarioId { get; private set; }

    public Guid? OrganizacaoId { get; private set; }

    public void Limpar()
    {
        Autenticado = false;
        EscopoValido = false;
        EhCidadao = false;
        UsuarioId = null;
        OrganizacaoId = null;
    }

    public void DefinirEscopo(Guid usuarioId, Guid? organizacaoId, bool ehCidadao)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("O usuário do contexto não pode ser vazio.", nameof(usuarioId));
        }

        if (ehCidadao && organizacaoId is not null)
        {
            throw new ArgumentException(
                "O cidadão não pode ter organização no contexto.",
                nameof(organizacaoId));
        }

        if (!ehCidadao && (organizacaoId is null || organizacaoId == Guid.Empty))
        {
            throw new ArgumentException(
                "O contexto de organização exige uma organização válida.",
                nameof(organizacaoId));
        }

        Autenticado = true;
        EscopoValido = true;
        EhCidadao = ehCidadao;
        UsuarioId = usuarioId;
        OrganizacaoId = organizacaoId;
    }

    public void DefinirAutenticacaoInvalida(Guid? usuarioId = null)
    {
        Autenticado = true;
        EscopoValido = false;
        EhCidadao = false;
        UsuarioId = usuarioId;
        OrganizacaoId = null;
    }
}
