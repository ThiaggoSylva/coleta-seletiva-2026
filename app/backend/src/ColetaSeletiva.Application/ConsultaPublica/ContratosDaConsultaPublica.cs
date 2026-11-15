using ColetaSeletiva.Application.Catalogo;
using ColetaSeletiva.Application.Pontos;

namespace ColetaSeletiva.Application.ConsultaPublica;

public sealed record BuscaPublicaDePontosEntrada(
    int? MunicipioId = null,
    Guid? TipoResiduoId = null,
    bool? AbertoAgora = null,
    double? Latitude = null,
    double? Longitude = null,
    double? RaioKm = null,
    int Pagina = 1,
    int TamanhoPagina = 20);

public sealed record PontoPublico(
    Guid Id,
    string Nome,
    string OrganizacaoNome,
    string? ContatoEmail,
    string? ContatoTelefone,
    MunicipioDoCatalogo Municipio,
    string Bairro,
    string Rua,
    string Numero,
    string? Complemento,
    string Cep,
    double Latitude,
    double Longitude,
    string? Observacoes,
    bool AbertoAgora,
    double? DistanciaKm,
    IReadOnlyList<HorarioDoPontoResposta> Horarios,
    IReadOnlyList<TipoDoPontoResposta> Tipos);

public sealed record PaginaDePontosPublicos(
    IReadOnlyList<PontoPublico> Itens,
    int Pagina,
    int TamanhoPagina,
    int Total);

public interface IConsultaPublicaDePontos
{
    Task<PaginaDePontosPublicos> BuscarAsync(
        BuscaPublicaDePontosEntrada entrada,
        CancellationToken cancellationToken = default);
}
