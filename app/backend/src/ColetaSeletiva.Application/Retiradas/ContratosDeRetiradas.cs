using FluentResults;

namespace ColetaSeletiva.Application.Retiradas;

public sealed record OrganizacaoParaRetiradaResposta(
    Guid Id,
    string Nome,
    string Tipo,
    string? ContatoEmail,
    string? ContatoTelefone,
    IReadOnlyList<TipoDomiciliarResposta> Tipos);

public sealed record TipoDomiciliarResposta(
    Guid TipoResiduoId,
    string Codigo,
    string Nome,
    string Categoria,
    string Unidade);

public sealed record JanelaDeRetiradaResposta(
    DateOnly Data,
    string Periodo,
    string HoraInicio,
    string HoraFim,
    int Capacidade,
    int Ocupacao,
    int Vagas);

public sealed record CriarRetiradaEntrada(
    Guid OrganizacaoId,
    int MunicipioId,
    string Bairro,
    string Rua,
    string Numero,
    string? Complemento,
    string Cep,
    string? PontoReferencia,
    string? Observacoes,
    DateOnly Data,
    string Periodo,
    IReadOnlyList<ItemDeRetiradaEntrada> Itens);

public sealed record ItemDeRetiradaEntrada(
    Guid TipoResiduoId,
    decimal QuantidadeEstimada);

public sealed record RetiradaResumo(
    Guid Id,
    string Estado,
    DateOnly Data,
    string Periodo,
    Guid OrganizacaoId,
    string OrganizacaoNome,
    int MunicipioId,
    string MunicipioNome,
    string Bairro,
    DateTimeOffset CriadoEmUtc);

public sealed record RetiradaDetalhe(
    Guid Id,
    string Estado,
    Guid OrganizacaoId,
    string OrganizacaoNome,
    string? NomeCidadao,
    string? TelefoneCidadao,
    bool DadosPessoaisOcultos,
    string MunicipioNome,
    string? Bairro,
    string? Rua,
    string? Numero,
    string? Complemento,
    string? Cep,
    string? PontoReferencia,
    string? Observacoes,
    DateOnly Data,
    string Periodo,
    string? Motivo,
    DateTimeOffset CriadoEmUtc,
    DateTimeOffset AtualizadoEmUtc,
    IReadOnlyList<ItemDaRetiradaResposta> Itens,
    IReadOnlyList<HistoricoDaRetiradaResposta> Historico,
    IReadOnlyList<VolumeDaRetiradaResposta> Volumes);

public sealed record ItemDaRetiradaResposta(
    Guid TipoResiduoId,
    string Codigo,
    string Nome,
    string Unidade,
    decimal QuantidadeEstimada);

public sealed record HistoricoDaRetiradaResposta(
    string? EstadoAnterior,
    string EstadoNovo,
    Guid AtorUsuarioId,
    DateTimeOffset OcorridoEmUtc,
    string? Motivo);

public sealed record VolumeDaRetiradaResposta(
    Guid TipoResiduoId,
    string Codigo,
    string Nome,
    string Unidade,
    decimal QuantidadeLiquida);

public sealed record PaginaDeRetiradas(
    IReadOnlyList<RetiradaResumo> Itens,
    int Pagina,
    int TamanhoPagina,
    int Total);

public sealed record FiltroDeRetiradasDaOrganizacao(
    string? Estado = null,
    DateOnly? Data = null,
    int? MunicipioId = null,
    int Pagina = 1,
    int TamanhoPagina = 20);

public sealed record AgendarRetiradaEntrada(
    DateOnly? Data = null,
    string? Periodo = null);

public sealed record ReagendarRetiradaEntrada(
    DateOnly Data,
    string Periodo);

public sealed record RecusarRetiradaEntrada(string Motivo);

public sealed record NaoRealizadaEntrada(string Motivo);

public sealed record ExecutarRetiradaEntrada(
    IReadOnlyList<ItemExecutadoEntrada> Itens);

public sealed record ItemExecutadoEntrada(
    Guid TipoResiduoId,
    decimal Quantidade);

public sealed record DisponibilidadeSemanaEntrada(
    IReadOnlyList<DisponibilidadeDiaEntrada> Dias);

public sealed record DisponibilidadeDiaEntrada(
    string DiaDaSemana,
    string Periodo,
    int Capacidade);

public sealed record DisponibilidadeDiaResposta(
    string DiaDaSemana,
    string Periodo,
    int Capacidade);

public sealed record BloqueioDeDataEntrada(
    DateOnly Data,
    string? Motivo);

public sealed record BloqueioDeDataResposta(
    Guid Id,
    DateOnly Data,
    string? Motivo,
    Guid CriadoPorUsuarioId,
    DateTimeOffset CriadoEmUtc);

public sealed record RegistrarRecebimentoEntrada(
    Guid PontoId,
    Guid TipoResiduoId,
    decimal Quantidade,
    DateTimeOffset? OcorridoEmUtc = null);

public sealed record AjustarVolumeEntrada(
    decimal Quantidade,
    string Motivo);

public sealed record VolumeResposta(
    Guid Id,
    Guid TipoResiduoId,
    string TipoCodigo,
    string TipoNome,
    string Unidade,
    decimal Quantidade,
    string Origem,
    Guid? SolicitacaoId,
    Guid? PontoId,
    string? PontoNome,
    Guid? RegistroOriginalId,
    decimal? TotalDoOriginal,
    Guid OperadorUsuarioId,
    string OperadorNome,
    DateTimeOffset OcorridoEmUtc,
    DateOnly DataLocal,
    string? Motivo);

public sealed record PaginaDeVolumes(
    IReadOnlyList<VolumeResposta> Itens,
    int Pagina,
    int TamanhoPagina,
    int Total);

public sealed record FiltroDeVolumes(
    DateOnly? De = null,
    DateOnly? Ate = null,
    Guid? TipoResiduoId = null,
    string? Origem = null,
    Guid? PontoId = null,
    int Pagina = 1,
    int TamanhoPagina = 20);

public sealed record FiltroDoPainel(
    DateOnly? De = null,
    DateOnly? Ate = null,
    Guid? TipoResiduoId = null,
    string? Origem = null);

public sealed record VolumePorTipoResposta(
    Guid TipoResiduoId,
    string Codigo,
    string Nome,
    string Unidade,
    decimal Quantidade);

public sealed record TotalPorUnidadeResposta(
    string Unidade,
    decimal Quantidade);

public sealed record ContagemPorEstadoResposta(
    string Estado,
    int Quantidade);

public sealed record VolumePorOrigemResposta(
    string Origem,
    string Unidade,
    decimal Quantidade);

public sealed record VolumePorPontoResposta(
    Guid PontoId,
    string PontoNome,
    string Unidade,
    decimal Quantidade);

public sealed record ResumoDoPainel(
    DateOnly De,
    DateOnly Ate,
    IReadOnlyList<VolumePorTipoResposta> PorTipo,
    IReadOnlyList<TotalPorUnidadeResposta> TotaisPorUnidade,
    IReadOnlyList<ContagemPorEstadoResposta> RetiradasPorEstado,
    IReadOnlyList<VolumePorOrigemResposta> PorOrigem,
    IReadOnlyList<VolumePorPontoResposta> PorPonto);

public sealed record PontoDaSerieResposta(
    DateOnly Inicio,
    IReadOnlyList<VolumePorTipoResposta> Itens);

public sealed record SerieDoPainel(
    string Agrupamento,
    DateOnly De,
    DateOnly Ate,
    IReadOnlyList<PontoDaSerieResposta> Pontos);

public sealed record ArquivoCsvResposta(
    string NomeArquivo,
    byte[] Conteudo);

public sealed record NotificacaoResposta(
    Guid Id,
    string Tipo,
    Guid ReferenciaId,
    string Titulo,
    string Mensagem,
    bool Lida,
    DateTimeOffset? LidaEmUtc,
    DateTimeOffset CriadaEmUtc);

public sealed record ListaDeNotificacoesResposta(
    IReadOnlyList<NotificacaoResposta> Itens,
    int NaoLidas);

public interface IRetiradas
{
    Task<IReadOnlyList<OrganizacaoParaRetiradaResposta>> ListarOrganizacoesElegiveisAsync(
        int municipioId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<JanelaDeRetiradaResposta>>> ListarJanelasAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken = default);

    Task<Result<RetiradaDetalhe>> CriarAsync(
        Guid cidadaoId,
        CriarRetiradaEntrada entrada,
        CancellationToken cancellationToken = default);

    Task<PaginaDeRetiradas> ListarMinhasAsync(
        Guid cidadaoId,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default);

    Task<Result<RetiradaDetalhe>> ObterMinhaAsync(
        Guid cidadaoId,
        Guid solicitacaoId,
        CancellationToken cancellationToken = default);

    Task<Result<RetiradaDetalhe>> CancelarAsync(
        Guid cidadaoId,
        Guid solicitacaoId,
        CancellationToken cancellationToken = default);

    Task<PaginaDeRetiradas> ListarAsync(
        Guid organizacaoId,
        FiltroDeRetiradasDaOrganizacao filtro,
        CancellationToken cancellationToken = default);

    Task<Result<RetiradaDetalhe>> ObterAsync(
        Guid organizacaoId,
        Guid solicitacaoId,
        CancellationToken cancellationToken = default);

    Task<Result<RetiradaDetalhe>> AgendarAsync(
        Guid organizacaoId,
        Guid atorUsuarioId,
        Guid solicitacaoId,
        AgendarRetiradaEntrada entrada,
        CancellationToken cancellationToken = default);

    Task<Result<RetiradaDetalhe>> ReagendarAsync(
        Guid organizacaoId,
        Guid atorUsuarioId,
        Guid solicitacaoId,
        ReagendarRetiradaEntrada entrada,
        CancellationToken cancellationToken = default);

    Task<Result<RetiradaDetalhe>> RecusarAsync(
        Guid organizacaoId,
        Guid atorUsuarioId,
        Guid solicitacaoId,
        RecusarRetiradaEntrada entrada,
        CancellationToken cancellationToken = default);

    Task<Result<RetiradaDetalhe>> ExecutarAsync(
        Guid organizacaoId,
        Guid atorUsuarioId,
        Guid solicitacaoId,
        ExecutarRetiradaEntrada entrada,
        CancellationToken cancellationToken = default);

    Task<Result<RetiradaDetalhe>> MarcarNaoRealizadaAsync(
        Guid organizacaoId,
        Guid atorUsuarioId,
        Guid solicitacaoId,
        NaoRealizadaEntrada entrada,
        CancellationToken cancellationToken = default);
}

public interface IDisponibilidadeDeRetirada
{
    Task<IReadOnlyList<DisponibilidadeDiaResposta>> ObterAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<DisponibilidadeDiaResposta>>> DefinirAsync(
        Guid organizacaoId,
        IReadOnlyCollection<DisponibilidadeDiaEntrada> dias,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BloqueioDeDataResposta>> ListarBloqueiosAsync(
        Guid organizacaoId,
        CancellationToken cancellationToken = default);

    Task<Result<BloqueioDeDataResposta>> CriarBloqueioAsync(
        Guid organizacaoId,
        Guid usuarioId,
        BloqueioDeDataEntrada entrada,
        CancellationToken cancellationToken = default);

    Task<Result> RemoverBloqueioAsync(
        Guid organizacaoId,
        Guid bloqueioId,
        CancellationToken cancellationToken = default);
}

public interface IVolumes
{
    Task<PaginaDeVolumes> ListarAsync(
        Guid organizacaoId,
        FiltroDeVolumes filtro,
        CancellationToken cancellationToken = default);

    Task<Result<VolumeResposta>> RegistrarRecebimentoAsync(
        Guid organizacaoId,
        Guid operadorUsuarioId,
        RegistrarRecebimentoEntrada entrada,
        CancellationToken cancellationToken = default);

    Task<Result<VolumeResposta>> AjustarAsync(
        Guid organizacaoId,
        Guid operadorUsuarioId,
        Guid volumeId,
        AjustarVolumeEntrada entrada,
        CancellationToken cancellationToken = default);
}

public interface IPainelDeVolumes
{
    Task<ResumoDoPainel> ObterResumoAsync(
        Guid organizacaoId,
        FiltroDoPainel filtro,
        CancellationToken cancellationToken = default);

    Task<Result<SerieDoPainel>> ObterSerieAsync(
        Guid organizacaoId,
        FiltroDoPainel filtro,
        string agrupamento,
        CancellationToken cancellationToken = default);

    Task<ArquivoCsvResposta> ExportarCsvAsync(
        Guid organizacaoId,
        FiltroDoPainel filtro,
        CancellationToken cancellationToken = default);
}

public interface INotificacoes
{
    Task<ListaDeNotificacoesResposta> ListarAsync(
        Guid usuarioId,
        DateTimeOffset? desde,
        bool apenasNaoLidas,
        CancellationToken cancellationToken = default);

    Task<Result<NotificacaoResposta>> MarcarComoLidaAsync(
        Guid usuarioId,
        Guid notificacaoId,
        CancellationToken cancellationToken = default);

    Task<Result<int>> MarcarTodasComoLidasAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);
}
