using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Infrastructure.Persistencia;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.Infrastructure.Acesso;

/// <summary>
/// Verifica a unicidade global de usuário e e-mail. O filtro de tenant esconde
/// contas de outras organizações, mas os índices únicos são globais; por isso a
/// consulta ignora os filtros e nunca devolve os valores encontrados.
/// </summary>
internal sealed class UnicidadeDeContas
{
    private readonly ColetaSeletivaDbContext _contexto;
    private readonly ILookupNormalizer _normalizador;

    public UnicidadeDeContas(
        ColetaSeletivaDbContext contexto,
        ILookupNormalizer normalizador)
    {
        _contexto = contexto;
        _normalizador = normalizador;
    }

    public async Task<IReadOnlyList<IError>> VerificarAsync(
        string usuario,
        string email,
        Guid? ignorarUsuarioId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var usuarioNormalizado = _normalizador.NormalizeName(usuario.Trim());
        var emailNormalizado = _normalizador.NormalizeEmail(email.Trim());

        var conflitos = await _contexto.Usuarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(outro => (ignorarUsuarioId == null || outro.Id != ignorarUsuarioId)
                && (outro.NormalizedUserName == usuarioNormalizado
                    || outro.NormalizedEmail == emailNormalizado))
            .Select(outro => new { outro.NormalizedUserName, outro.NormalizedEmail })
            .ToListAsync(cancellationToken);

        var erros = new List<IError>();
        var usuarioEhEmail = string.Equals(
            usuarioNormalizado,
            emailNormalizado,
            StringComparison.Ordinal);
        var conflitoDeUsuario = conflitos.Any(conflito => string.Equals(
            conflito.NormalizedUserName,
            usuarioNormalizado,
            StringComparison.Ordinal));
        var conflitoDeEmail = conflitos.Any(conflito => string.Equals(
            conflito.NormalizedEmail,
            emailNormalizado,
            StringComparison.Ordinal));

        if (!usuarioEhEmail && conflitoDeUsuario)
        {
            erros.Add(new ErroDeConflito(
                CodigosDeErro.UsuarioEmUso,
                "O usuário informado já está em uso."));
        }

        if (conflitoDeEmail || (usuarioEhEmail && conflitoDeUsuario))
        {
            erros.Add(new ErroDeConflito(
                CodigosDeErro.EmailEmUso,
                "O e-mail informado já está em uso."));
        }

        return erros;
    }

    public static bool EhViolacaoDeUnicidade(DbUpdateException excecao)
    {
        ArgumentNullException.ThrowIfNull(excecao);

        return excecao.InnerException is SqlException sql
            && sql.Errors.Cast<SqlError>().Any(erro => erro.Number is 2601 or 2627);
    }
}
