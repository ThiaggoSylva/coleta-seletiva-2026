namespace ColetaSeletiva.Domain.Entidades;

/// <summary>
/// Intervalo de funcionamento persistido de um ponto de coleta (RN-08).
/// </summary>
public sealed class PontoHorario : IEntidadeDaOrganizacao
{
    private PontoHorario()
    {
    }

    private PontoHorario(Guid id, Guid pontoColetaId, HorarioDeFuncionamento horario)
    {
        Id = id;
        PontoColetaId = pontoColetaId;
        DiaDaSemana = horario.DiaDaSemana;
        HoraInicio = horario.HoraInicio;
        HoraFim = horario.HoraFim;
    }

    public Guid Id { get; private set; }

    public Guid OrganizacaoId { get; private set; }

    public Guid PontoColetaId { get; private set; }

    public DayOfWeek DiaDaSemana { get; private set; }

    public TimeOnly HoraInicio { get; private set; }

    public TimeOnly HoraFim { get; private set; }

    Guid? IEntidadeDaOrganizacao.OrganizacaoId
    {
        get => OrganizacaoId == Guid.Empty ? null : OrganizacaoId;
        set => OrganizacaoId = value
            ?? throw new ArgumentNullException(nameof(value), "A organização do horário é obrigatória.");
    }

    internal static PontoHorario Criar(Guid pontoColetaId, HorarioDeFuncionamento horario)
    {
        if (pontoColetaId == Guid.Empty)
        {
            throw new ArgumentException("O ponto do horário é obrigatório.", nameof(pontoColetaId));
        }

        PontoDeColeta.ValidarHorario(horario.DiaDaSemana, horario.HoraInicio, horario.HoraFim);

        return new PontoHorario(Guid.NewGuid(), pontoColetaId, horario);
    }

    internal bool EstaAbertoAgora(DayOfWeek dia, TimeOnly hora)
    {
        return DiaDaSemana == dia && hora >= HoraInicio && hora < HoraFim;
    }
}
