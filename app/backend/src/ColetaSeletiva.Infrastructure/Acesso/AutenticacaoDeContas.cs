using ColetaSeletiva.Application.Acesso.Sessao;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Domain.Identidade;
using ColetaSeletiva.Infrastructure.Identidade;
using FluentResults;
using Microsoft.AspNetCore.Identity;

namespace ColetaSeletiva.Infrastructure.Acesso;

internal sealed class AutenticacaoDeContas : IAutenticacaoDeContas
{
    private readonly UserManager<Usuario> _usuarios;

    public AutenticacaoDeContas(UserManager<Usuario> usuarios)
    {
        _usuarios = usuarios;
    }

    public async Task<Result<ContaAutenticada>> AutenticarAsync(
        string identificador,
        string senha,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identificador);
        ArgumentNullException.ThrowIfNull(senha);

        var identificadorNormalizado = identificador.Trim();
        var usuario = await LocalizarAsync(identificadorNormalizado);
        if (usuario is null || !usuario.Ativo)
        {
            return FalhaComCredenciais();
        }

        if (!await _usuarios.CheckPasswordAsync(usuario, senha))
        {
            return FalhaComCredenciais();
        }

        var conta = await MontarContaAsync(usuario);
        if (conta is null)
        {
            return FalhaComCredenciais();
        }

        var entrouPorEmail = string.Equals(
            usuario.Email,
            identificadorNormalizado,
            StringComparison.OrdinalIgnoreCase);

        if (conta.Papel == Papeis.Cidadao && !entrouPorEmail)
        {
            return FalhaComCredenciais();
        }

        return Result.Ok(conta);
    }

    public async Task<Result<ContaAutenticada>> ObterPorUsuarioIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
        {
            return Result.Fail<ContaAutenticada>(
                new ErroNaoAutenticado(MensagensDeSessao.SessaoInvalida));
        }

        var conta = await MontarContaAsync(usuario);
        if (conta is null)
        {
            return Result.Fail<ContaAutenticada>(
                new ErroNaoAutenticado(MensagensDeSessao.SessaoInvalida));
        }

        return Result.Ok(conta);
    }

    private async Task<Usuario?> LocalizarAsync(string identificador)
    {
        var porEmail = await _usuarios.FindByEmailAsync(identificador);
        if (porEmail is not null)
        {
            return porEmail;
        }

        return await _usuarios.FindByNameAsync(identificador);
    }

    private async Task<ContaAutenticada?> MontarContaAsync(Usuario usuario)
    {
        var papeis = await _usuarios.GetRolesAsync(usuario);
        var papel = Papeis.Todos.FirstOrDefault(papeis.Contains);
        if (papel is null)
        {
            return null;
        }

        if (papel == Papeis.Cidadao)
        {
            if (usuario.OrganizacaoId is not null)
            {
                return null;
            }
        }
        else if (usuario.OrganizacaoId is null)
        {
            return null;
        }

        return new ContaAutenticada(
            Id: usuario.Id,
            Nome: usuario.Nome,
            Email: usuario.Email ?? string.Empty,
            Papel: papel,
            OrganizacaoId: usuario.OrganizacaoId,
            TrocaSenhaPendente: usuario.TrocaSenhaPendente,
            Ativa: usuario.Ativo);
    }

    private static Result<ContaAutenticada> FalhaComCredenciais()
    {
        return Result.Fail<ContaAutenticada>(
            new ErroNaoAutenticado(MensagensDeSessao.CredenciaisInvalidas));
    }
}
