using ColetaSeletiva.Application.Erros;
using FluentResults;
using Microsoft.AspNetCore.Identity;

namespace ColetaSeletiva.Infrastructure.Acesso;

internal static class MapeamentoDeErrosIdentity
{
    public static IEnumerable<IError> MapearVarios(IReadOnlyCollection<IdentityError> erros)
    {
        ArgumentNullException.ThrowIfNull(erros);

        // Para o cidadão, usuário e e-mail são o mesmo valor. Quando o e-mail
        // duplicado já explica o conflito, não repetimos o erro de usuário.
        var efetivos = erros.Any(erro => erro.Code == "DuplicateEmail")
            ? erros.Where(erro => erro.Code != "DuplicateUserName")
            : erros;

        return efetivos.Select(Mapear);
    }

    public static IError Mapear(IdentityError erro)
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
