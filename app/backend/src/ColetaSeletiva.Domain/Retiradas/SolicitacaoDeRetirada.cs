using ColetaSeletiva.Domain.Entidades;

namespace ColetaSeletiva.Domain.Retiradas;

/// <summary>
/// Estados da solicitação de retirada (RF-12–RF-14; RN-03).
/// </summary>
public enum EstadoSolicitacao
{
    Solicitada,
    Agendada,
    Coletada,
    NaoRealizada,
    Recusada,
    Cancelada,
}

/// <summary>
/// Motivos fechados de não realização (RF-14).
/// </summary>
public enum MotivoNaoRealizacao
{
    CidadaoAusente,
    MaterialInadequado,
    EnderecoNaoLocalizado,
    ImpossibilidadeOperacional,
}

/// <summary>
/// Item estimado informado pelo cidadão no momento da solicitação (RF-11).
/// </summary>
public sealed class SolicitacaoItem
{
    private SolicitacaoItem()
    {
    }

    private SolicitacaoItem(Guid id, Guid tipoResiduoId, decimal quantidadeEstimada)
    {
        Id = id;
        TipoResiduoId = tipoResiduoId;
        QuantidadeEstimada = quantidadeEstimada;
    }

    public Guid Id { get; private set; }

    public Guid SolicitacaoId { get; private set; }

    public Guid TipoResiduoId { get; private set; }

    public decimal QuantidadeEstimada { get; private set; }

    internal static SolicitacaoItem Criar(Guid tipoResiduoId, decimal quantidadeEstimada)
    {
        if (tipoResiduoId == Guid.Empty)
        {
            throw new ArgumentException("O tipo de resíduo é obrigatório.", nameof(tipoResiduoId));
        }

        if (!QuantidadeValida(quantidadeEstimada) || quantidadeEstimada <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantidadeEstimada),
                quantidadeEstimada,
                "A quantidade estimada deve ser positiva, com até três casas decimais.");
        }

        return new SolicitacaoItem(Guid.NewGuid(), tipoResiduoId, quantidadeEstimada);
    }

    public static bool QuantidadeValida(decimal quantidade)
    {
        return quantidade == decimal.Round(quantidade, 3, MidpointRounding.ToZero)
            && quantidade <= 999_999_999.999m;
    }
}

/// <summary>
/// Linha imutável do histórico de estados da solicitação (RF-13; RN-03).
/// </summary>
public sealed class SolicitacaoHistorico
{
    private SolicitacaoHistorico()
    {
    }

    private SolicitacaoHistorico(
        Guid id,
        EstadoSolicitacao? estadoAnterior,
        EstadoSolicitacao estadoNovo,
        Guid atorUsuarioId,
        DateTimeOffset ocorridoEmUtc,
        string? motivo)
    {
        Id = id;
        EstadoAnterior = estadoAnterior;
        EstadoNovo = estadoNovo;
        AtorUsuarioId = atorUsuarioId;
        OcorridoEmUtc = ocorridoEmUtc.ToUniversalTime();
        Motivo = motivo;
    }

    public Guid Id { get; private set; }

    public Guid SolicitacaoId { get; private set; }

    public EstadoSolicitacao? EstadoAnterior { get; private set; }

    public EstadoSolicitacao EstadoNovo { get; private set; }

    public Guid AtorUsuarioId { get; private set; }

    public DateTimeOffset OcorridoEmUtc { get; private set; }

    public string? Motivo { get; private set; }

    internal static SolicitacaoHistorico Criar(
        EstadoSolicitacao? estadoAnterior,
        EstadoSolicitacao estadoNovo,
        Guid atorUsuarioId,
        DateTimeOffset agora,
        string? motivo)
    {
        return new SolicitacaoHistorico(
            Guid.NewGuid(),
            estadoAnterior,
            estadoNovo,
            atorUsuarioId,
            agora,
            motivo);
    }
}

/// <summary>
/// Solicitação de retirada domiciliar com snapshot de dados pessoais (RF-11–RF-14; RN-11).
/// </summary>
public sealed class SolicitacaoDeRetirada
{
    public const int NomeMaxLength = 150;
    public const int TelefoneMaxLength = 20;
    public const int UfLength = 2;
    public const int MunicipioNomeMaxLength = 120;
    public const int BairroMaxLength = 120;
    public const int RuaMaxLength = 150;
    public const int NumeroMaxLength = 20;
    public const int ComplementoMaxLength = 80;
    public const int CepLength = 8;
    public const int ReferenciaMaxLength = 300;
    public const int ObservacoesMaxLength = 500;
    public const int MotivoMaxLength = 500;

    private SolicitacaoDeRetirada()
    {
    }

    private SolicitacaoDeRetirada(
        Guid id,
        Guid organizacaoId,
        Guid cidadaoId,
        string nomeCidadao,
        string? telefoneCidadao,
        string uf,
        int municipioId,
        string municipioNome,
        string bairro,
        string rua,
        string numero,
        string? complemento,
        string cep,
        string? pontoReferencia,
        string? observacoes,
        DateOnly dataLocal,
        PeriodoDeRetirada periodo,
        DateTimeOffset agora)
    {
        Id = id;
        OrganizacaoId = organizacaoId;
        CidadaoId = cidadaoId;
        NomeCidadao = nomeCidadao;
        TelefoneCidadao = telefoneCidadao;
        Uf = uf;
        MunicipioId = municipioId;
        MunicipioNome = municipioNome;
        Bairro = bairro;
        Rua = rua;
        Numero = numero;
        Complemento = complemento;
        Cep = cep;
        PontoReferencia = pontoReferencia;
        Observacoes = observacoes;
        DataLocal = dataLocal;
        Periodo = periodo;
        Estado = EstadoSolicitacao.Solicitada;
        CriadoEmUtc = agora.ToUniversalTime();
        AtualizadoEmUtc = CriadoEmUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrganizacaoId { get; private set; }

    public Guid CidadaoId { get; private set; }

    public string NomeCidadao { get; private set; } = string.Empty;

    public string? TelefoneCidadao { get; private set; }

    public string Uf { get; private set; } = string.Empty;

    public int MunicipioId { get; private set; }

    public string MunicipioNome { get; private set; } = string.Empty;

    public string Bairro { get; private set; } = string.Empty;

    public string Rua { get; private set; } = string.Empty;

    public string Numero { get; private set; } = string.Empty;

    public string? Complemento { get; private set; }

    public string Cep { get; private set; } = string.Empty;

    public string? PontoReferencia { get; private set; }

    public string? Observacoes { get; private set; }

    public DateOnly DataLocal { get; private set; }

    public PeriodoDeRetirada Periodo { get; private set; }

    public EstadoSolicitacao Estado { get; private set; }

    public string? Motivo { get; private set; }

    public DateTimeOffset CriadoEmUtc { get; private set; }

    public DateTimeOffset AtualizadoEmUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public List<SolicitacaoItem> Itens { get; private set; } = [];

    public List<SolicitacaoHistorico> Historico { get; private set; } = [];

    public bool EstaAberta => Estado is EstadoSolicitacao.Solicitada or EstadoSolicitacao.Agendada;

    public bool PodeExibirDadosPessoais => EstaAberta;

    public static SolicitacaoDeRetirada Criar(
        Guid organizacaoId,
        Guid cidadaoId,
        string nomeCidadao,
        string? telefoneCidadao,
        string uf,
        int municipioId,
        string municipioNome,
        string bairro,
        string rua,
        string numero,
        string? complemento,
        string cep,
        string? pontoReferencia,
        string? observacoes,
        DateOnly dataLocal,
        PeriodoDeRetirada periodo,
        IReadOnlyCollection<(Guid TipoResiduoId, decimal QuantidadeEstimada)> itens,
        Guid atorUsuarioId,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(itens);

        if (organizacaoId == Guid.Empty)
        {
            throw new ArgumentException("A organização é obrigatória.", nameof(organizacaoId));
        }

        if (cidadaoId == Guid.Empty)
        {
            throw new ArgumentException("O cidadão é obrigatório.", nameof(cidadaoId));
        }

        if (itens.Count == 0)
        {
            throw new ArgumentException("Informe ao menos um tipo de resíduo.", nameof(itens));
        }

        var distintos = itens.Select(item => item.TipoResiduoId).Distinct().ToList();
        if (distintos.Count != itens.Count)
        {
            throw new ArgumentException("Não repita tipos de resíduo.", nameof(itens));
        }

        if (!Enum.IsDefined(periodo))
        {
            throw new ArgumentOutOfRangeException(nameof(periodo), periodo, "Período inválido.");
        }

        var solicitacao = new SolicitacaoDeRetirada(
            Guid.NewGuid(),
            organizacaoId,
            cidadaoId,
            NormalizarTexto(nomeCidadao, NomeMaxLength, nameof(nomeCidadao)),
            NormalizarOpcional(telefoneCidadao, TelefoneMaxLength, nameof(telefoneCidadao)),
            NormalizarUf(uf),
            ValidarMunicipio(municipioId),
            NormalizarTexto(municipioNome, MunicipioNomeMaxLength, nameof(municipioNome)),
            NormalizarTexto(bairro, BairroMaxLength, nameof(bairro)),
            NormalizarTexto(rua, RuaMaxLength, nameof(rua)),
            NormalizarTexto(numero, NumeroMaxLength, nameof(numero)),
            NormalizarOpcional(complemento, ComplementoMaxLength, nameof(complemento)),
            NormalizarCep(cep),
            NormalizarOpcional(pontoReferencia, ReferenciaMaxLength, nameof(pontoReferencia)),
            NormalizarOpcional(observacoes, ObservacoesMaxLength, nameof(observacoes)),
            dataLocal,
            periodo,
            agora);

        foreach (var item in itens)
        {
            solicitacao.Itens.Add(SolicitacaoItem.Criar(item.TipoResiduoId, item.QuantidadeEstimada));
        }

        solicitacao.Historico.Add(SolicitacaoHistorico.Criar(
            estadoAnterior: null,
            estadoNovo: EstadoSolicitacao.Solicitada,
            atorUsuarioId,
            agora,
            motivo: null));

        return solicitacao;
    }

    public void Agendar(Guid atorUsuarioId, DateTimeOffset agora)
    {
        ExigirEstado(EstadoSolicitacao.Solicitada, "agendar");
        Transicionar(EstadoSolicitacao.Agendada, atorUsuarioId, agora, motivo: null);
    }

    public void Reagendar(
        DateOnly dataLocal,
        PeriodoDeRetirada periodo,
        Guid atorUsuarioId,
        DateTimeOffset agora)
    {
        ExigirEstado(EstadoSolicitacao.Agendada, "reagendar");

        if (!Enum.IsDefined(periodo))
        {
            throw new ArgumentOutOfRangeException(nameof(periodo), periodo, "Período inválido.");
        }

        DataLocal = dataLocal;
        Periodo = periodo;
        AtualizadoEmUtc = agora.ToUniversalTime();
        Historico.Add(SolicitacaoHistorico.Criar(
            EstadoSolicitacao.Agendada,
            EstadoSolicitacao.Agendada,
            atorUsuarioId,
            agora,
            "Reagendada"));
    }

    public void Recusar(string motivo, Guid atorUsuarioId, DateTimeOffset agora)
    {
        ExigirEstado(EstadoSolicitacao.Solicitada, "recusar");
        var normalizado = ExigirMotivo(motivo, nameof(motivo));
        Transicionar(EstadoSolicitacao.Recusada, atorUsuarioId, agora, normalizado);
    }

    public void Cancelar(Guid atorUsuarioId, DateTimeOffset agora)
    {
        if (!EstaAberta)
        {
            throw new InvalidOperationException(
                "Somente solicitações Solicitada ou Agendada podem ser canceladas.");
        }

        Transicionar(EstadoSolicitacao.Cancelada, atorUsuarioId, agora, "Cancelada pelo cidadão");
    }

    public void MarcarNaoRealizada(
        MotivoNaoRealizacao motivo,
        Guid atorUsuarioId,
        DateTimeOffset agora)
    {
        ExigirEstado(EstadoSolicitacao.Agendada, "marcar como não realizada");

        if (!Enum.IsDefined(motivo))
        {
            throw new ArgumentOutOfRangeException(nameof(motivo), motivo, "Motivo inválido.");
        }

        Transicionar(
            EstadoSolicitacao.NaoRealizada,
            atorUsuarioId,
            agora,
            motivo.ToString());
    }

    public void MarcarColetada(Guid atorUsuarioId, DateTimeOffset agora)
    {
        ExigirEstado(EstadoSolicitacao.Agendada, "registrar execução");
        Transicionar(EstadoSolicitacao.Coletada, atorUsuarioId, agora, motivo: null);
    }

    private void Transicionar(
        EstadoSolicitacao novo,
        Guid atorUsuarioId,
        DateTimeOffset agora,
        string? motivo)
    {
        var anterior = Estado;
        Estado = novo;
        Motivo = motivo;
        AtualizadoEmUtc = agora.ToUniversalTime();

        Historico.Add(SolicitacaoHistorico.Criar(anterior, novo, atorUsuarioId, agora, motivo));
    }

    private void ExigirEstado(EstadoSolicitacao esperado, string operacao)
    {
        if (Estado != esperado)
        {
            throw new InvalidOperationException(
                $"Não é possível {operacao} uma solicitação no estado {Estado}.");
        }
    }

    private static int ValidarMunicipio(int municipioId)
    {
        if (municipioId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(municipioId),
                municipioId,
                "O município é obrigatório.");
        }

        return municipioId;
    }

    private static string NormalizarTexto(string valor, int tamanhoMaximo, string parametro)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor, parametro);

        var normalizado = valor.Trim();
        if (normalizado.Length > tamanhoMaximo)
        {
            throw new ArgumentException(
                $"O valor deve ter no máximo {tamanhoMaximo} caracteres.",
                parametro);
        }

        return normalizado;
    }

    private static string? NormalizarOpcional(string? valor, int tamanhoMaximo, string parametro)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? null
            : NormalizarTexto(valor, tamanhoMaximo, parametro);
    }

    private static string NormalizarUf(string uf)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uf);

        var normalizada = uf.Trim().ToUpperInvariant();
        if (normalizada.Length != UfLength || !normalizada.All(char.IsLetter))
        {
            throw new ArgumentException("A UF deve ter duas letras.", nameof(uf));
        }

        return normalizada;
    }

    private static string NormalizarCep(string cep)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cep);

        var digitos = new string(cep.Where(char.IsDigit).ToArray());
        if (digitos.Length != CepLength)
        {
            throw new ArgumentException("O CEP deve ter oito dígitos.", nameof(cep));
        }

        return digitos;
    }

    private static string ExigirMotivo(string motivo, string parametro)
    {
        var normalizado = NormalizarTexto(motivo, MotivoMaxLength, parametro);
        return normalizado;
    }
}
