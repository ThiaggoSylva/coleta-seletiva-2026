using ColetaSeletiva.Application.Acesso;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Infrastructure.Identidade;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.Infrastructure.Acesso;

internal sealed class CadastroDeContas : ICadastroDeContas
{
    private readonly UserManager<Usuario> _usuarios;
    private readonly UnicidadeDeContas _unicidade;
    private readonly TimeProvider _relogio;

    public CadastroDeContas(
        UserManager<Usuario> usuarios,
        UnicidadeDeContas unicidade,
        TimeProvider relogio)
    {
        _usuarios = usuarios;
        _unicidade = unicidade;
        _relogio = relogio;
    }

    public async Task<Result<Guid>> CriarAsync(
        ContaNova conta,
        string papel,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conta);
        ArgumentException.ThrowIfNullOrWhiteSpace(papel);

        var conflitos = await _unicidade.VerificarAsync(
            conta.Usuario,
            conta.Email,
            ignorarUsuarioId: null,
            cancellationToken);

        if (conflitos.Count > 0)
        {
            return Result.Fail<Guid>(conflitos);
        }

        var agora = _relogio.GetUtcNow();
        var usuario = new Usuario
        {
            UserName = conta.Usuario.Trim(),
            Email = conta.Email.Trim(),
            Nome = conta.Nome.Trim(),
            Telefone = conta.Telefone,
            OrganizacaoId = conta.OrganizacaoId,
            Ativo = true,
            TrocaSenhaPendente = conta.TrocaSenhaPendente,
            CriadoEmUtc = agora,
            AtualizadoEmUtc = agora,
        };

        IdentityResult criacao;
        try
        {
            criacao = await _usuarios.CreateAsync(usuario, conta.Senha);
        }
        catch (DbUpdateException excecao) when (UnicidadeDeContas.EhViolacaoDeUnicidade(excecao))
        {
            return Result.Fail<Guid>(
                new ErroDeConflito(
                    CodigosDeErro.UsuarioEmUso,
                    "O usuário ou e-mail informado já está em uso."));
        }

        if (!criacao.Succeeded)
        {
            return Result.Fail<Guid>(MapeamentoDeErrosIdentity.MapearVarios(criacao.Errors.ToList()));
        }

        var atribuicao = await _usuarios.AddToRoleAsync(usuario, papel);
        if (!atribuicao.Succeeded)
        {
            return Result.Fail<Guid>(MapeamentoDeErrosIdentity.MapearVarios(atribuicao.Errors.ToList()));
        }

        return Result.Ok(usuario.Id);
    }
}
