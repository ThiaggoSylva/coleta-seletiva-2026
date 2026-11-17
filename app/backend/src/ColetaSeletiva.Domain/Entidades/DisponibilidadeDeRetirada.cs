namespace ColetaSeletiva.Domain.Entidades;

/// <summary>
/// Capacidade de retiradas por dia da semana e período (RF-10; RN-02).
/// </summary>
public sealed class DisponibilidadeDeRetirada : IEntidadeDaOrganizacao
{
    public const int CapacidadeMaxima = 1000;

    private DisponibilidadeDeRetirada()
    {
    }

    private DisponibilidadeDeRetirada(
        Guid id,
        DayOfWeek diaDaSemana,
        PeriodoDeRetirada periodo,
        int capacidade,
        DateTimeOffset agora)
    {
        Id = id;
        DiaDaSemana = diaDaSemana;
        Periodo = periodo;
        Capacidade = capacidade;
        CriadoEmUtc = agora.ToUniversalTime();
        AtualizadoEmUtc = CriadoEmUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrganizacaoId { get; private set; }

    public DayOfWeek DiaDaSemana { get; private set; }

    public PeriodoDeRetirada Periodo { get; private set; }

    public int Capacidade { get; private set; }

    public DateTimeOffset CriadoEmUtc { get; private set; }

    public DateTimeOffset AtualizadoEmUtc { get; private set; }

    Guid? IEntidadeDaOrganizacao.OrganizacaoId
    {
        get => OrganizacaoId == Guid.Empty ? null : OrganizacaoId;
        set => OrganizacaoId = value
            ?? throw new ArgumentNullException(
                nameof(value),
                "A organização da disponibilidade é obrigatória.");
    }

    public static DisponibilidadeDeRetirada Criar(
        DayOfWeek diaDaSemana,
        PeriodoDeRetirada periodo,
        int capacidade,
        DateTimeOffset agora)
    {
        Validar(diaDaSemana, periodo, capacidade);

        return new DisponibilidadeDeRetirada(
            Guid.NewGuid(),
            diaDaSemana,
            periodo,
            capacidade,
            agora);
    }

    public void DefinirCapacidade(int capacidade, DateTimeOffset agora)
    {
        Validar(DiaDaSemana, Periodo, capacidade);

        Capacidade = capacidade;
        AtualizadoEmUtc = agora.ToUniversalTime();
    }

    private static void Validar(
        DayOfWeek diaDaSemana,
        PeriodoDeRetirada periodo,
        int capacidade)
    {
        if (!Enum.IsDefined(diaDaSemana))
        {
            throw new ArgumentOutOfRangeException(
                nameof(diaDaSemana),
                diaDaSemana,
                "Dia da semana inválido.");
        }

        if (!Enum.IsDefined(periodo))
        {
            throw new ArgumentOutOfRangeException(nameof(periodo), periodo, "Período inválido.");
        }

        if (capacidade is < 0 or > CapacidadeMaxima)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacidade),
                capacidade,
                $"A capacidade deve estar entre 0 e {CapacidadeMaxima}.");
        }
    }
}
