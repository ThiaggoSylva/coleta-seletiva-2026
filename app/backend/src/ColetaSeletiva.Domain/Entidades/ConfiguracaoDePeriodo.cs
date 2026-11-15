namespace ColetaSeletiva.Domain.Entidades;

/// <summary>
/// Período de retirada configurado pela organização (RF-06, RN-01).
/// </summary>
public enum PeriodoDeRetirada
{
    Manha,
    Tarde,
}

/// <summary>
/// Intervalo local de um período de retirada. O fim deve ser maior que o início no mesmo dia.
/// </summary>
public sealed class ConfiguracaoDePeriodo : IEntidadeDaOrganizacao
{
    private ConfiguracaoDePeriodo()
    {
    }

    private ConfiguracaoDePeriodo(
        Guid id,
        PeriodoDeRetirada periodo,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        DateTimeOffset agora)
    {
        Id = id;
        Periodo = periodo;
        HoraInicio = horaInicio;
        HoraFim = horaFim;
        CriadoEmUtc = agora.ToUniversalTime();
        AtualizadoEmUtc = CriadoEmUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrganizacaoId { get; private set; }

    public PeriodoDeRetirada Periodo { get; private set; }

    public TimeOnly HoraInicio { get; private set; }

    public TimeOnly HoraFim { get; private set; }

    public DateTimeOffset CriadoEmUtc { get; private set; }

    public DateTimeOffset AtualizadoEmUtc { get; private set; }

    Guid? IEntidadeDaOrganizacao.OrganizacaoId
    {
        get => OrganizacaoId == Guid.Empty ? null : OrganizacaoId;
        set => OrganizacaoId = value
            ?? throw new ArgumentNullException(nameof(value), "A organização do período é obrigatória.");
    }

    public static ConfiguracaoDePeriodo Criar(
        PeriodoDeRetirada periodo,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        DateTimeOffset agora)
    {
        if (!Enum.IsDefined(periodo))
        {
            throw new ArgumentOutOfRangeException(nameof(periodo), periodo, "Período inválido.");
        }

        if (horaFim <= horaInicio)
        {
            throw new ArgumentException(
                "A hora final do período deve ser maior que a inicial no mesmo dia.",
                nameof(horaFim));
        }

        return new ConfiguracaoDePeriodo(Guid.NewGuid(), periodo, horaInicio, horaFim, agora);
    }

    public void Atualizar(TimeOnly horaInicio, TimeOnly horaFim, DateTimeOffset agora)
    {
        if (horaFim <= horaInicio)
        {
            throw new ArgumentException(
                "A hora final do período deve ser maior que a inicial no mesmo dia.",
                nameof(horaFim));
        }

        HoraInicio = horaInicio;
        HoraFim = horaFim;
        AtualizadoEmUtc = agora.ToUniversalTime();
    }
}
