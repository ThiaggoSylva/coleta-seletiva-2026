using ColetaSeletiva.Application.Acesso;
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
            Telefone = conta.Telefone,
            OrganizacaoId = conta.OrganizacaoId,
            Ativo = true,
            TrocaSenhaPendente = conta.TrocaSenhaPendente,
            CriadoEmUtc = agora,
            AtualizadoEmUtc = agora,
        };

        var criacao = await _usuarios.CreateAsync(usuario, conta.Senha);
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
