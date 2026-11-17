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

/// <summary>
/// Ponto exibido na consulta pública. Pontos de fontes externas não pertencem a uma
/// organização e podem não ter endereço estruturado, horário conhecido ou tipos aceitos.
/// </summary>
public sealed record PontoPublico(
    string Id,
    OrigemDoPonto Origem,
    string Nome,
    string OrganizacaoNome,
    string? ContatoEmail,
    string? ContatoTelefone,
    MunicipioDoCatalogo? Municipio,
    string? Bairro,
    string? Rua,
    string? Numero,
    string? Complemento,
    string? Cep,
    double Latitude,
    double Longitude,
    string? Observacoes,
    bool? AbertoAgora,
    double? DistanciaKm,
    IReadOnlyList<HorarioDoPontoResposta> Horarios,
    IReadOnlyList<TipoDoPontoResposta> Tipos,
    string? Atribuicao = null,
    string? UrlDaFonte = null);

public sealed record PaginaDePontosPublicos(
    IReadOnlyList<PontoPublico> Itens,
    int Pagina,
    int TamanhoPagina,
    int Total,
    string? AvisoDeFonteExterna = null,
    string? AtribuicaoDeFonteExterna = null);

public interface IConsultaPublicaDePontos
{
    Task<PaginaDePontosPublicos> BuscarAsync(
        BuscaPublicaDePontosEntrada entrada,
        CancellationToken cancellationToken = default);
}
