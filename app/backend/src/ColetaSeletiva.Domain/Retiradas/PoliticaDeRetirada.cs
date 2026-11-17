namespace ColetaSeletiva.Domain.Retiradas;

using ColetaSeletiva.Domain.Entidades;

/// <summary>
/// Regras temporais compartilhadas pelas retiradas (RF-10, RF-11; RN-01, RN-03).
/// </summary>
public static class PoliticaDeRetirada
{
    public const int DiasDeAntecedenciaMinima = 1;
    public const int DiasDeHorizonteMaximo = 30;
    public const int HorasMinimasParaCancelamento = 24;

    public static DateOnly HojeNoFuso(DateTimeOffset agora, TimeZoneInfo fuso)
    {
        ArgumentNullException.ThrowIfNull(fuso);

        var instanteLocal = TimeZoneInfo.ConvertTime(agora, fuso);
        return DateOnly.FromDateTime(instanteLocal.DateTime);
    }

    /// <summary>
    /// Aceita de amanhã até hoje + 30 dias, conforme a seção 2.2 do PRD.
    /// </summary>
    public static bool DentroDoHorizonte(DateOnly hoje, DateOnly data)
    {
        var antecedencia = data.DayNumber - hoje.DayNumber;
        return antecedencia >= DiasDeAntecedenciaMinima
            && antecedencia <= DiasDeHorizonteMaximo;
    }

    public static DateTimeOffset InicioDoPeriodo(
        DateOnly data,
        TimeOnly horaInicio,
        TimeZoneInfo fuso)
    {
        ArgumentNullException.ThrowIfNull(fuso);

        var local = new DateTime(
            data.Year,
            data.Month,
            data.Day,
            horaInicio.Hour,
            horaInicio.Minute,
            horaInicio.Second,
            DateTimeKind.Unspecified);
        var utc = TimeZoneInfo.ConvertTimeToUtc(local, fuso);

        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    /// <summary>
    /// O cidadão pode cancelar enquanto faltarem ao menos 24 horas para o início local do período.
    /// </summary>
    public static bool PodeCancelar(
        DateTimeOffset agora,
        DateOnly data,
        TimeOnly horaInicio,
        TimeZoneInfo fuso)
    {
        var inicio = InicioDoPeriodo(data, horaInicio, fuso);
        var prazo = inicio.AddHours(-HorasMinimasParaCancelamento);

        return agora <= prazo;
    }

    public static TimeOnly HoraInicioPadrao(PeriodoDeRetirada periodo)
    {
        return periodo == PeriodoDeRetirada.Manha
            ? new TimeOnly(8, 0)
            : new TimeOnly(13, 0);
    }

    public static TimeOnly HoraFimPadrao(PeriodoDeRetirada periodo)
    {
        return periodo == PeriodoDeRetirada.Manha
            ? new TimeOnly(12, 0)
            : new TimeOnly(17, 0);
    }
}
