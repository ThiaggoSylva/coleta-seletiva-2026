using ColetaSeletiva.Application.Acesso.Sessao;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Infrastructure.Identidade;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ColetaSeletiva.Infrastructure.Acesso;

internal sealed class AlteracaoDeSenha : IAlteracaoDeSenha
{
    private const string CampoNovaSenha = "NovaSenha";

    private readonly UserManager<Usuario> _usuarios;
    private readonly IdentityOptions _opcoes;
    private readonly TimeProvider _relogio;

    public AlteracaoDeSenha(
        UserManager<Usuario> usuarios,
        IOptions<IdentityOptions> opcoes,
        TimeProvider relogio)
    {
        _usuarios = usuarios;
        _opcoes = opcoes.Value;
        _relogio = relogio;
    }

    public async Task<Result> AlterarAsync(
        Guid usuarioId,
        string senhaAtual,
        string novaSenha,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("O usuário da troca de senha não pode ser vazio.", nameof(usuarioId));
        }

        ArgumentNullException.ThrowIfNull(senhaAtual);
        ArgumentNullException.ThrowIfNull(novaSenha);

        var usuario = await _usuarios.FindByIdAsync(usuarioId.ToString());
        if (usuario is null || !usuario.Ativo)
        {
            return Result.Fail(
                new ErroNaoAutenticado(MensagensDeSessao.SessaoInvalida));
        }

        var alteracao = await _usuarios.ChangePasswordAsync(usuario, senhaAtual, novaSenha);
        if (!alteracao.Succeeded)
        {
            return MapearFalha(alteracao.Errors);
        }

        usuario.TrocaSenhaPendente = false;
        usuario.AtualizadoEmUtc = _relogio.GetUtcNow();

        var atualizacao = await _usuarios.UpdateAsync(usuario);
        if (!atualizacao.Succeeded)
        {
            return Result.Fail(
                new ErroInterno("Não foi possível concluir a alteração da senha."));
        }

        return Result.Ok();
    }

    private Result MapearFalha(IEnumerable<IdentityError> erros)
    {
        var lista = erros.ToList();

        if (lista.Any(erro => erro.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
        {
            return Result.Fail(
                new ErroDeValidacao(
                    nameof(AlterarSenhaEntrada.SenhaAtual),
                    "A senha atual informada está incorreta."));
        }

        return Result.Fail(lista.Select(MapearErroDePolitica));
    }

    private IError MapearErroDePolitica(IdentityError erro)
    {
        var politica = _opcoes.Password;

        return erro.Code switch
        {
            nameof(IdentityErrorDescriber.PasswordTooShort) =>
                new ErroDeValidacao(
                    CampoNovaSenha,
                    $"A nova senha deve ter pelo menos {politica.RequiredLength} caracteres."),
            nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars) =>
                new ErroDeValidacao(
                    CampoNovaSenha,
                    $"A nova senha deve conter ao menos {politica.RequiredUniqueChars} caracteres distintos."),
            nameof(IdentityErrorDescriber.PasswordRequiresDigit) =>
                new ErroDeValidacao(
                    CampoNovaSenha,
                    "A nova senha deve conter ao menos um dígito."),
            nameof(IdentityErrorDescriber.PasswordRequiresLower) =>
                new ErroDeValidacao(
                    CampoNovaSenha,
                    "A nova senha deve conter ao menos uma letra minúscula."),
            nameof(IdentityErrorDescriber.PasswordRequiresUpper) =>
                new ErroDeValidacao(
                    CampoNovaSenha,
                    "A nova senha deve conter ao menos uma letra maiúscula."),
            nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric) =>
                new ErroDeValidacao(
                    CampoNovaSenha,
                    "A nova senha deve conter ao menos um caractere especial."),
            _ => new ErroDeValidacao(
                CampoNovaSenha,
                "A nova senha não atende à política de segurança."),
        };
    }
}
