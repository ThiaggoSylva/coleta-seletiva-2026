using ColetaSeletiva.Application.Acesso.PerfilDoCidadao;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.Infrastructure.Acesso;

internal sealed class PerfilDeCidadao : IPerfilDeCidadao
{
    private readonly UserManager<Usuario> _usuarios;
    private readonly ColetaSeletivaDbContext _contexto;
    private readonly ILookupNormalizer _normalizador;
    private readonly TimeProvider _relogio;

    public PerfilDeCidadao(
        UserManager<Usuario> usuarios,
        ColetaSeletivaDbContext contexto,
        ILookupNormalizer normalizador,
        TimeProvider relogio)
    {
        _usuarios = usuarios;
        _contexto = contexto;
        _normalizador = normalizador;
        _relogio = relogio;
    }

    public async Task<Result<PerfilDoCidadao>> ObterAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.FindByIdAsync(usuarioId.ToString());
        if (usuario is null || usuario.OrganizacaoId is not null)
        {
            return Result.Fail<PerfilDoCidadao>(
                new ErroNaoEncontrado("Perfil do cidadão não encontrado."));
        }

        return Result.Ok(Mapear(usuario));
    }

    public async Task<Result<PerfilDoCidadao>> AtualizarAsync(
        Guid usuarioId,
        AtualizarPerfilDoCidadaoEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var usuario = await _usuarios.FindByIdAsync(usuarioId.ToString());
        if (usuario is null || usuario.OrganizacaoId is not null)
        {
            return Result.Fail<PerfilDoCidadao>(
                new ErroNaoEncontrado("Perfil do cidadão não encontrado."));
        }

        var email = entrada.Email.Trim();

        // O filtro global do cidadão esconde outras contas; a unicidade de e-mail é
        // global, então a checagem precisa ignorar o filtro de tenant.
        var emailNormalizado = _normalizador.NormalizeEmail(email);
        var usuarioNormalizado = _normalizador.NormalizeName(email);
        var emailEmUso = await _contexto.Usuarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                outro => outro.Id != usuarioId
                    && (outro.NormalizedEmail == emailNormalizado
                        || outro.NormalizedUserName == usuarioNormalizado),
                cancellationToken);

        if (emailEmUso)
        {
            return Result.Fail<PerfilDoCidadao>(
                new ErroDeConflito(
                    CodigosDeErro.EmailEmUso,
                    "O e-mail informado já está em uso."));
        }

        usuario.Nome = entrada.Nome.Trim();
        usuario.Telefone = string.IsNullOrWhiteSpace(entrada.Telefone)
            ? null
            : entrada.Telefone.Trim();
        usuario.Email = email;
        usuario.UserName = email;
        usuario.AtualizadoEmUtc = _relogio.GetUtcNow();

        IdentityResult atualizacao;
        try
        {
            atualizacao = await _usuarios.UpdateAsync(usuario);
        }
        catch (DbUpdateException excecao) when (EhViolacaoDeUnicidade(excecao))
        {
            return Result.Fail<PerfilDoCidadao>(
                new ErroDeConflito(
                    CodigosDeErro.EmailEmUso,
                    "O e-mail informado já está em uso."));
        }

        if (!atualizacao.Succeeded)
        {
            return Result.Fail<PerfilDoCidadao>(atualizacao.Errors.Select(MapearErro));
        }

        return Result.Ok(Mapear(usuario));
    }

    private static PerfilDoCidadao Mapear(Usuario usuario)
    {
        return new PerfilDoCidadao(
            usuario.Id,
            usuario.Nome,
            usuario.Email ?? string.Empty,
            usuario.Telefone);
    }

    private static IError MapearErro(IdentityError erro)
    {
        return erro.Code switch
        {
            "DuplicateUserName" or "DuplicateEmail" => new ErroDeConflito(
                CodigosDeErro.EmailEmUso,
                "O e-mail informado já está em uso."),
            "InvalidUserName" or "InvalidEmail" => new ErroDeValidacao(
                "Email",
                "O e-mail informado é inválido."),
            _ => MapeamentoDeErrosIdentity.Mapear(erro),
        };
    }

    private static bool EhViolacaoDeUnicidade(DbUpdateException excecao)
    {
        return excecao.InnerException is SqlException sql
            && sql.Errors.Cast<SqlError>().Any(erro => erro.Number is 2601 or 2627);
    }
}
