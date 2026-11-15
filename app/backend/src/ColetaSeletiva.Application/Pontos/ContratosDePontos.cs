using ColetaSeletiva.Application.Catalogo;
using ColetaSeletiva.Domain.Entidades;
using FluentResults;

namespace ColetaSeletiva.Application.Pontos;

public sealed record HorarioDoPontoEntrada(
    string DiaDaSemana,
    string HoraInicio,
    string HoraFim);

public sealed record HorarioDoPontoResposta(
    string DiaDaSemana,
    string HoraInicio,
    string HoraFim);

public sealed record TipoDoPontoResposta(
    Guid TipoResiduoId,
    string Codigo,
    string Nome,
    string Unidade);

public sealed record PontoResumo(
    Guid Id,
    string Nome,
    string Municipio,
    string Uf,
    string Status,
    bool AbertoAgora,
    bool Excluido,
    IReadOnlyList<string> Tipos);

public sealed record PontoDetalhe(
    Guid Id,
    string Nome,
    MunicipioDoCatalogo Municipio,
    string Uf,
    string Bairro,
    string Rua,
    string Numero,
    string? Complemento,
    string Cep,
    double Latitude,
    double Longitude,
    string? Observacoes,
    string Status,
    bool AbertoAgora,
    bool Excluido,
    IReadOnlyList<HorarioDoPontoResposta> Horarios,
    IReadOnlyList<TipoDoPontoResposta> Tipos);

public sealed record CriarPontoEntrada(
    string Nome,
    int MunicipioId,
    string Bairro,
    string Rua,
    string Numero,
    string? Complemento,
    string Cep,
    double Latitude,
    double Longitude,
    string? Observacoes,
    string Status,
    IReadOnlyList<HorarioDoPontoEntrada> Horarios,
    IReadOnlyList<Guid> TiposResiduoIds);

public sealed record AtualizarPontoEntrada(
    string Nome,
    int MunicipioId,
    string Bairro,
    string Rua,
    string Numero,
    string? Complemento,
    string Cep,
    double Latitude,
    double Longitude,
    string? Observacoes,
    string Status,
    IReadOnlyList<HorarioDoPontoEntrada> Horarios,
    IReadOnlyList<Guid> TiposResiduoIds);

public sealed record AlterarStatusPontoEntrada(
    string Status);

public sealed record OpcaoDePonto(
    Guid Id,
    string Nome,
    string Municipio,
    string Uf);

public interface IPontosDeColeta
{
    Task<IReadOnlyList<PontoResumo>> ListarAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken = default);

    Task<Result<PontoDetalhe>> ObterAsync(
        Guid organizacaoId,
        Guid pontoId,
        CancellationToken cancellationToken = default);

    Task<Result<PontoDetalhe>> CriarAsync(
        Guid organizacaoId,
        CriarPontoEntrada entrada,
        CancellationToken cancellationToken = default);

    Task<Result<PontoDetalhe>> AtualizarAsync(
        Guid organizacaoId,
        Guid pontoId,
        AtualizarPontoEntrada entrada,
        CancellationToken cancellationToken = default);

    Task<Result<PontoDetalhe>> AlterarStatusAsync(
        Guid organizacaoId,
        Guid pontoId,
        StatusPonto status,
        CancellationToken cancellationToken = default);

    Task<Result<PontoDetalhe>> RetirarAsync(
        Guid organizacaoId,
        Guid pontoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpcaoDePonto>> ListarOpcoesAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken = default);
}
