using NetTopologySuite.Geometries;

namespace ColetaSeletiva.Domain.Entidades;

/// <summary>
/// Situação operacional do ponto de coleta (RF-08).
/// </summary>
public enum StatusPonto
{
    Ativo,
    Inativo,
    IndisponivelTemporariamente,
}

/// <summary>
/// Intervalo de funcionamento de um ponto em um dia da semana, sem atravessar a meia-noite (RN-08).
/// </summary>
public readonly record struct HorarioDeFuncionamento(
    DayOfWeek DiaDaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFim)
{
    public bool EstaNoIntervalo(DayOfWeek dia, TimeOnly hora)
    {
        return dia == DiaDaSemana && hora >= HoraInicio && hora < HoraFim;
    }
}

/// <summary>
/// Ponto de coleta de uma organização (RF-08). A exclusão é sempre lógica (RF-22).
/// </summary>
public sealed class PontoDeColeta : IEntidadeDaOrganizacao
{
    public const int NomeMaxLength = 120;
    public const int UfLength = 2;
    public const int BairroMaxLength = 120;
    public const int RuaMaxLength = 150;
    public const int NumeroMaxLength = 20;
    public const int ComplementoMaxLength = 80;
    public const int ObservacoesMaxLength = 500;
    public const int CepLength = 8;
    public const double LatitudeMinima = -90;
    public const double LatitudeMaxima = 90;
    public const double LongitudeMinima = -180;
    public const double LongitudeMaxima = 180;
    public const int SridWgs84 = 4326;

    private PontoDeColeta()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizacaoId { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    /// <summary>Código IBGE do município, obrigatoriamente atendido pela organização.</summary>
    public int MunicipioId { get; private set; }

    public string Uf { get; private set; } = string.Empty;

    public string Bairro { get; private set; } = string.Empty;

    public string Rua { get; private set; } = string.Empty;

    public string Numero { get; private set; } = string.Empty;

    public string? Complemento { get; private set; }

    public string Cep { get; private set; } = string.Empty;

    /// <summary>Localização em WGS 84 (`geography` SRID 4326).</summary>
    public Point Localizacao { get; private set; } = null!;

    public double Latitude => Localizacao.Y;

    public double Longitude => Localizacao.X;

    public string? Observacoes { get; private set; }

    public StatusPonto Status { get; private set; }

    public DateTimeOffset? ExcluidoEmUtc { get; private set; }

    public DateTimeOffset CriadoEmUtc { get; private set; }

    public DateTimeOffset AtualizadoEmUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public List<PontoHorario> Horarios { get; private set; } = [];

    public List<PontoTipo> Tipos { get; private set; } = [];

    Guid? IEntidadeDaOrganizacao.OrganizacaoId
    {
        get => OrganizacaoId == Guid.Empty ? null : OrganizacaoId;
        set => OrganizacaoId = value
            ?? throw new ArgumentNullException(nameof(value), "A organização do ponto é obrigatória.");
    }

    public bool EstaAberto(DateTimeOffset agoraUtc, TimeZoneInfo fuso)
    {
        ArgumentNullException.ThrowIfNull(fuso);

        if (Status != StatusPonto.Ativo || ExcluidoEmUtc is not null)
        {
            return false;
        }

        var instanteLocal = TimeZoneInfo.ConvertTime(agoraUtc, fuso);
        var hora = TimeOnly.FromDateTime(instanteLocal.DateTime);

        return Horarios.Any(horario => horario.EstaAbertoAgora(instanteLocal.DayOfWeek, hora));
    }

    public static PontoDeColeta Criar(
        string nome,
        int municipioId,
        string uf,
        string bairro,
        string rua,
        string numero,
        string? complemento,
        string cep,
        double latitude,
        double longitude,
        string? observacoes,
        StatusPonto status,
        IReadOnlyCollection<HorarioDeFuncionamento> horarios,
        IReadOnlyCollection<Guid> tiposResiduoIds,
        DateTimeOffset agora)
    {
        var ponto = new PontoDeColeta
        {
            Id = Guid.NewGuid(),
            CriadoEmUtc = agora.ToUniversalTime(),
        };

        ponto.Aplicar(
            nome,
            municipioId,
            uf,
            bairro,
            rua,
            numero,
            complemento,
            cep,
            latitude,
            longitude,
            observacoes,
            status,
            horarios,
            tiposResiduoIds,
            agora);

        return ponto;
    }

    public void Atualizar(
        string nome,
        int municipioId,
        string uf,
        string bairro,
        string rua,
        string numero,
        string? complemento,
        string cep,
        double latitude,
        double longitude,
        string? observacoes,
        StatusPonto status,
        IReadOnlyCollection<HorarioDeFuncionamento> horarios,
        IReadOnlyCollection<Guid> tiposResiduoIds,
        DateTimeOffset agora)
    {
        if (ExcluidoEmUtc is not null)
        {
            throw new InvalidOperationException("Ponto retirado logicamente não pode ser alterado.");
        }

        Aplicar(
            nome,
            municipioId,
            uf,
            bairro,
            rua,
            numero,
            complemento,
            cep,
            latitude,
            longitude,
            observacoes,
            status,
            horarios,
            tiposResiduoIds,
            agora);
    }

    public void AlterarStatus(StatusPonto status, DateTimeOffset agora)
    {
        if (ExcluidoEmUtc is not null)
        {
            throw new InvalidOperationException("Ponto retirado logicamente não pode ser alterado.");
        }

        ValidarStatus(status);
        Status = status;
        AtualizadoEmUtc = agora.ToUniversalTime();
    }

    public void Retirar(DateTimeOffset agora)
    {
        if (ExcluidoEmUtc is not null)
        {
            return;
        }

        ExcluidoEmUtc = agora.ToUniversalTime();
        AtualizadoEmUtc = ExcluidoEmUtc.Value;
    }

    internal static HorarioDeFuncionamento ValidarHorario(
        DayOfWeek diaDaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFim)
    {
        if (!Enum.IsDefined(diaDaSemana))
        {
            throw new ArgumentOutOfRangeException(
                nameof(diaDaSemana),
                diaDaSemana,
                "Dia da semana inválido.");
        }

        if (horaFim <= horaInicio)
        {
            throw new ArgumentException(
                "O horário deve começar e terminar no mesmo dia, com fim maior que o início.",
                nameof(horaFim));
        }

        return new HorarioDeFuncionamento(diaDaSemana, horaInicio, horaFim);
    }

    private void Aplicar(
        string nome,
        int municipioId,
        string uf,
        string bairro,
        string rua,
        string numero,
        string? complemento,
        string cep,
        double latitude,
        double longitude,
        string? observacoes,
        StatusPonto status,
        IReadOnlyCollection<HorarioDeFuncionamento> horarios,
        IReadOnlyCollection<Guid> tiposResiduoIds,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(horarios);
        ArgumentNullException.ThrowIfNull(tiposResiduoIds);

        if (municipioId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(municipioId), municipioId, "O município é obrigatório.");
        }

        ValidarStatus(status);

        if (horarios.Count == 0)
        {
            throw new ArgumentException("Informe ao menos um horário de funcionamento.", nameof(horarios));
        }

        if (tiposResiduoIds.Count == 0)
        {
            throw new ArgumentException("Informe ao menos um tipo aceito.", nameof(tiposResiduoIds));
        }

        if (tiposResiduoIds.Any(tipo => tipo == Guid.Empty))
        {
            throw new ArgumentException("Tipo de resíduo inválido.", nameof(tiposResiduoIds));
        }

        var tiposDistintos = tiposResiduoIds.Distinct().ToList();
        if (tiposDistintos.Count != tiposResiduoIds.Count)
        {
            throw new ArgumentException("Não repita tipos aceitos.", nameof(tiposResiduoIds));
        }

        if (latitude is < LatitudeMinima or > LatitudeMaxima)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude fora do intervalo válido.");
        }

        if (longitude is < LongitudeMinima or > LongitudeMaxima)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude fora do intervalo válido.");
        }

        Nome = NormalizarTexto(nome, NomeMaxLength, nameof(nome));
        MunicipioId = municipioId;
        Uf = NormalizarUf(uf);
        Bairro = NormalizarTexto(bairro, BairroMaxLength, nameof(bairro));
        Rua = NormalizarTexto(rua, RuaMaxLength, nameof(rua));
        Numero = NormalizarTexto(numero, NumeroMaxLength, nameof(numero));
        Complemento = NormalizarOpcional(complemento, ComplementoMaxLength, nameof(complemento));
        Cep = NormalizarCep(cep);
        Observacoes = NormalizarOpcional(observacoes, ObservacoesMaxLength, nameof(observacoes));
        Status = status;
        Localizacao = new Point(longitude, latitude) { SRID = SridWgs84 };
        AtualizadoEmUtc = agora.ToUniversalTime();

        SubstituirHorarios(horarios);
        SubstituirTipos(tiposDistintos);
    }

    private void SubstituirHorarios(IReadOnlyCollection<HorarioDeFuncionamento> horarios)
    {
        Horarios.Clear();

        foreach (var horario in horarios)
        {
            Horarios.Add(PontoHorario.Criar(Id, horario));
        }
    }

    private void SubstituirTipos(IReadOnlyCollection<Guid> tiposResiduoIds)
    {
        Tipos.Clear();

        foreach (var tipoResiduoId in tiposResiduoIds)
        {
            Tipos.Add(PontoTipo.Criar(Id, tipoResiduoId));
        }
    }

    private static void ValidarStatus(StatusPonto status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Status do ponto inválido.");
        }
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
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return NormalizarTexto(valor, tamanhoMaximo, parametro);
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
}
