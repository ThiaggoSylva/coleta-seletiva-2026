using ColetaSeletiva.Application.Acesso;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Infrastructure.Identidade;
using FluentResults;
using Microsoft.AspNetCore.Identity;

namespace ColetaSeletiva.Infrastructure.Acesso;

internal sealed class CadastroDeContas : ICadastroDeContas
{
    private readonly UserManager<Usuario> _usuarios;
    private readonly TimeProvider _relogio;

    public CadastroDeContas(UserManager<Usuario> usuarios, TimeProvider relogio)
    {
        _usuarios = usuarios;
        _relogio = relogio;
    }

    public async Task<Result<Guid>> CriarAsync(
        ContaNova conta,
        string papel,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conta);
        ArgumentException.ThrowIfNullOrWhiteSpace(papel);

        var agora = _relogio.GetUtcNow();
        var usuario = new Usuario
        {
            UserName = conta.Usuario,
            Email = conta.Email,
            Nome = conta.Nome,
            OrganizacaoId = conta.OrganizacaoId,
            Ativo = true,
            TrocaSenhaPendente = conta.TrocaSenhaPendente,
            CriadoEmUtc = agora,
            AtualizadoEmUtc = agora,
        };

        var criacao = await _usuarios.CreateAsync(usuario, conta.Senha);
        if (!criacao.Succeeded)
        {
            return Result.Fail<Guid>(criacao.Errors.Select(MapearErro));
        }

        var atribuicao = await _usuarios.AddToRoleAsync(usuario, papel);
        if (!atribuicao.Succeeded)
        {
            return Result.Fail<Guid>(atribuicao.Errors.Select(MapearErro));
        }

        return Result.Ok(usuario.Id);
    }

    private static IError MapearErro(IdentityError erro)
    {
        return erro.Code switch
        {
            "DuplicateUserName" => new ErroDeConflito(
                CodigosDeErro.UsuarioEmUso,
                "O usuário informado já está em uso."),
            "DuplicateEmail" => new ErroDeConflito(
                CodigosDeErro.EmailEmUso,
                "O e-mail informado já está em uso."),
            "InvalidUserName" => new ErroDeValidacao(
                "Usuario",
                "O usuário informado é inválido."),
            "InvalidEmail" => new ErroDeValidacao(
                "Email",
                "O e-mail informado é inválido."),
            _ when erro.Code.StartsWith("Password", StringComparison.Ordinal) => new ErroDeValidacao(
                "Senha",
                "A senha não atende à política de segurança."),
            _ => new ErroInterno("Não foi possível criar a conta."),
        };
    }
}
