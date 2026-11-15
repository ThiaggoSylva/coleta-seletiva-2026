using ColetaSeletiva.Application.Catalogo;
using ColetaSeletiva.Domain.Entidades;
using FluentResults;

namespace ColetaSeletiva.Application.PerfilDaOrganizacao;

public sealed record PerfilDaOrganizacaoResposta(
    Guid Id,
    string Nome,
    string Tipo,
    string? ContatoEmail,
    string? ContatoTelefone,
    string Fuso,
    int LimiteSolicitacoesAbertas,
    bool Publicada,
    IReadOnlyList<MunicipioDoCatalogo> Municipios,
    IReadOnlyList<PeriodoDaOrganizacaoResposta> Periodos,
    IReadOnlyList<TipoHabilitadoResposta> Tipos);

public sealed record PeriodoDaOrganizacaoResposta(
    string Periodo,
    string HoraInicio,
    string HoraFim);

public sealed record TipoHabilitadoResposta(
    Guid TipoResiduoId,
    string Codigo,
    string Nome,
    string Categoria,
    string Unidade,
    bool AceitaRetiradaDomiciliar);

public sealed record AtualizarPerfilDaOrganizacaoEntrada(
    string Nome,
    TipoOrganizacao Tipo,
    string? ContatoEmail,
    string? ContatoTelefone,
    string Fuso,
    int LimiteSolicitacoesAbertas);

public sealed record DefinirMunicipiosEntrada(
    IReadOnlyList<int> MunicipioIds);

public sealed record DefinirPeriodosEntrada(
    IReadOnlyList<PeriodoDaOrganizacaoEntrada> Periodos);

public sealed record PeriodoDaOrganizacaoEntrada(
    string Periodo,
    string HoraInicio,
    string HoraFim);

public sealed record DefinirTiposHabilitadosEntrada(
    IReadOnlyList<TipoHabilitadoEntrada> Tipos);

public sealed record TipoHabilitadoEntrada(
    Guid TipoResiduoId,
    bool AceitaRetiradaDomiciliar);

public sealed record PublicacaoDaOrganizacaoEntrada(
    bool Publicada);

public sealed record PublicacaoDaOrganizacaoResposta(
    bool Publicada,
    DateTimeOffset AtualizadoEmUtc);

public interface IPerfilDaOrganizacao
{
    Task<PerfilDaOrganizacaoResposta?> ObterAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken = default);

    Task<Result<PerfilDaOrganizacaoResposta>> AtualizarAsync(
        Guid organizacaoId,
        AtualizarPerfilDaOrganizacaoEntrada entrada,
        CancellationToken cancellationToken = default);

    Task<Result<PerfilDaOrganizacaoResposta>> DefinirMunicipiosAsync(
        Guid organizacaoId,
        IReadOnlyCollection<int> municipioIds,
        CancellationToken cancellationToken = default);

    Task<Result<PerfilDaOrganizacaoResposta>> DefinirPeriodosAsync(
        Guid organizacaoId,
        IReadOnlyCollection<PeriodoDaOrganizacaoEntrada> periodos,
        CancellationToken cancellationToken = default);

    Task<Result<PerfilDaOrganizacaoResposta>> DefinirTiposAsync(
        Guid organizacaoId,
        IReadOnlyCollection<TipoHabilitadoEntrada> tipos,
        CancellationToken cancellationToken = default);

    Task<Result<PublicacaoDaOrganizacaoResposta>> DefinirPublicacaoAsync(
        Guid organizacaoId,
        bool publicada,
        CancellationToken cancellationToken = default);
}
