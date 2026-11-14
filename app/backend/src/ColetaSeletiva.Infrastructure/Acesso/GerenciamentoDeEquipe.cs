using ColetaSeletiva.Application.Acesso;
using ColetaSeletiva.Application.Acesso.Equipe;
using ColetaSeletiva.Application.Acesso.Sessao;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Domain.Identidade;
using ColetaSeletiva.Infrastructure.Identidade;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.Infrastructure.Acesso;

internal sealed class GerenciamentoDeEquipe : IGerenciamentoDeEquipe
{
    private readonly UserManager<Usuario> _usuarios;
    private readonly ICadastroDeContas _contas;
    private readonly UnicidadeDeContas _unicidade;
    private readonly IRepositorioDeRefreshTokens _refreshTokens;
    private readonly TimeProvider _relogio;

    public GerenciamentoDeEquipe(
        UserManager<Usuario> usuarios,
        ICadastroDeContas contas,
        UnicidadeDeContas unicidade,
        IRepositorioDeRefreshTokens refreshTokens,
        TimeProvider relogio)
    {
        _usuarios = usuarios;
        _contas = contas;
        _unicidade = unicidade;
        _refreshTokens = refreshTokens;
        _relogio = relogio;
    }

    public async Task<Result<ContaDaEquipe>> CriarAsync(
        Guid organizacaoId,
        CriarOperadorEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        ArgumentOutOfRangeException.ThrowIfEqual(organizacaoId, Guid.Empty);

        var conta = new ContaNova(
            Nome: entrada.Nome.Trim(),
            Usuario: entrada.Usuario.Trim(),
            Email: entrada.Email.Trim(),
            Telefone: null,
            Senha: entrada.Senha,
            OrganizacaoId: organizacaoId,
            TrocaSenhaPendente: true);

        var criacao = await _contas.CriarAsync(
            conta,
            Papeis.Operador,
            cancellationToken);

        if (criacao.IsFailed)
        {
            return Result.Fail<ContaDaEquipe>(criacao.Errors);
        }

        var operador = await ObterOperadorAsync(
            organizacaoId,
            criacao.Value,
            cancellationToken);

        if (operador is null)
        {
            throw new InvalidOperationException(
                "A conta de operador criada não pôde ser recuperada.");
        }

        return Result.Ok(Mapear(operador));
    }

    public async Task<Result<ContaDaEquipe>> ObterAsync(
        Guid organizacaoId,
        Guid operadorId,
        CancellationToken cancellationToken = default)
    {
        var operador = await ObterOperadorAsync(
            organizacaoId,
            operadorId,
            cancellationToken);

        return operador is null
            ? Result.Fail<ContaDaEquipe>(OperadorNaoEncontrado())
            : Result.Ok(Mapear(operador));
    }

    public async Task<Result<IReadOnlyList<ContaDaEquipe>>> ListarAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var operadores = await _usuarios.GetUsersInRoleAsync(Papeis.Operador);

        return Result.Ok<IReadOnlyList<ContaDaEquipe>>(
            operadores
                .Where(operador => operador.OrganizacaoId == organizacaoId)
                .OrderBy(operador => operador.Nome, StringComparer.OrdinalIgnoreCase)
                .Select(Mapear)
                .ToList());
    }

    public async Task<Result<ContaDaEquipe>> AtualizarAsync(
        Guid organizacaoId,
        Guid operadorId,
        EditarOperadorEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var operador = await ObterOperadorAsync(
            organizacaoId,
            operadorId,
            cancellationToken);

        if (operador is null)
        {
            return Result.Fail<ContaDaEquipe>(OperadorNaoEncontrado());
        }

        var conflitos = await _unicidade.VerificarAsync(
            entrada.Usuario,
            entrada.Email,
            operador.Id,
            cancellationToken);

        if (conflitos.Count > 0)
        {
            return Result.Fail<ContaDaEquipe>(conflitos);
        }

        operador.Nome = entrada.Nome.Trim();
        operador.UserName = entrada.Usuario.Trim();
        operador.Email = entrada.Email.Trim();
        operador.AtualizadoEmUtc = _relogio.GetUtcNow();

        IdentityResult atualizacao;
        try
        {
            atualizacao = await _usuarios.UpdateAsync(operador);
        }
        catch (DbUpdateException excecao) when (UnicidadeDeContas.EhViolacaoDeUnicidade(excecao))
        {
            return Result.Fail<ContaDaEquipe>(
                new ErroDeConflito(
                    CodigosDeErro.UsuarioEmUso,
                    "O usuário ou e-mail informado já está em uso."));
        }

        if (!atualizacao.Succeeded)
        {
            return Result.Fail<ContaDaEquipe>(
                MapeamentoDeErrosIdentity.MapearVarios(atualizacao.Errors.ToList()));
        }

        return Result.Ok(Mapear(operador));
    }

    public async Task<Result<ContaDaEquipe>> InativarAsync(
        Guid organizacaoId,
        Guid operadorId,
        CancellationToken cancellationToken = default)
    {
        var operador = await ObterOperadorAsync(
            organizacaoId,
            operadorId,
            cancellationToken);

        if (operador is null)
        {
            return Result.Fail<ContaDaEquipe>(OperadorNaoEncontrado());
        }

        var agora = _relogio.GetUtcNow();
        operador.Ativo = false;
        operador.AtualizadoEmUtc = agora;

        var atualizacao = await _usuarios.UpdateAsync(operador);
        if (!atualizacao.Succeeded)
        {
            return Result.Fail<ContaDaEquipe>(
                MapeamentoDeErrosIdentity.MapearVarios(atualizacao.Errors.ToList()));
        }

        await _refreshTokens.RevogarTodasAsync(
            operador.Id,
            agora,
            cancellationToken);

        return Result.Ok(Mapear(operador));
    }

    private async Task<Usuario?> ObterOperadorAsync(
        Guid organizacaoId,
        Guid operadorId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var operador = await _usuarios.FindByIdAsync(operadorId.ToString());
        if (operador is null || operador.OrganizacaoId != organizacaoId)
        {
            return null;
        }

        var papeis = await _usuarios.GetRolesAsync(operador);

        return papeis.Count == 1 && papeis[0] == Papeis.Operador
            ? operador
            : null;
    }

    private static ContaDaEquipe Mapear(Usuario operador)
    {
        return new ContaDaEquipe(
            Id: operador.Id,
            Nome: operador.Nome,
            Usuario: operador.UserName ?? string.Empty,
            Email: operador.Email ?? string.Empty,
            Ativo: operador.Ativo,
            TrocaSenhaPendente: operador.TrocaSenhaPendente);
    }

    private static ErroNaoEncontrado OperadorNaoEncontrado()
    {
        return new ErroNaoEncontrado("Operador não encontrado.");
    }
}
