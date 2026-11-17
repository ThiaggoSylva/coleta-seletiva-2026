using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Retiradas;

namespace ColetaSeletiva.Domain.Tests.Retiradas;

[TestClass]
public sealed class PoliticaDeRetiradaTests
{
    private static readonly TimeZoneInfo FusoDeSaoPaulo =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    [TestMethod]
    public void Horizonte_aceita_de_amanha_ate_trinta_dias()
    {
        var hoje = new DateOnly(2026, 11, 15);

        Assert.IsFalse(PoliticaDeRetirada.DentroDoHorizonte(hoje, hoje));
        Assert.IsFalse(PoliticaDeRetirada.DentroDoHorizonte(hoje, hoje.AddDays(-1)));
        Assert.IsTrue(PoliticaDeRetirada.DentroDoHorizonte(hoje, hoje.AddDays(1)));
        Assert.IsTrue(PoliticaDeRetirada.DentroDoHorizonte(hoje, hoje.AddDays(30)));
        Assert.IsFalse(PoliticaDeRetirada.DentroDoHorizonte(hoje, hoje.AddDays(31)));
    }

    [TestMethod]
    public void Cancelamento_exige_vinte_e_quatro_horas_antes_do_inicio_local()
    {
        var data = new DateOnly(2026, 11, 20);
        var inicioManha = new TimeOnly(8, 0);
        var prazo = new DateTimeOffset(2026, 11, 19, 11, 0, 0, TimeSpan.Zero);

        Assert.IsTrue(
            PoliticaDeRetirada.PodeCancelar(prazo, data, inicioManha, FusoDeSaoPaulo));
        Assert.IsFalse(
            PoliticaDeRetirada.PodeCancelar(
                prazo.AddSeconds(1),
                data,
                inicioManha,
                FusoDeSaoPaulo));
    }

    [TestMethod]
    public void Cancelamento_de_periodo_da_tarde_usa_o_horario_da_tarde()
    {
        var data = new DateOnly(2026, 11, 20);
        var inicioTarde = new TimeOnly(13, 0);

        Assert.IsTrue(
            PoliticaDeRetirada.PodeCancelar(
                new DateTimeOffset(2026, 11, 19, 16, 0, 0, TimeSpan.Zero),
                data,
                inicioTarde,
                FusoDeSaoPaulo));
        Assert.IsFalse(
            PoliticaDeRetirada.PodeCancelar(
                new DateTimeOffset(2026, 11, 19, 16, 0, 1, TimeSpan.Zero),
                data,
                inicioTarde,
                FusoDeSaoPaulo));
    }

    [TestMethod]
    public void Hoje_no_fuso_considera_a_data_local_e_nao_a_utc()
    {
        // 2026-11-16 01:30 UTC = 2026-11-15 22:30 em São Paulo.
        var agora = new DateTimeOffset(2026, 11, 16, 1, 30, 0, TimeSpan.Zero);

        Assert.AreEqual(
            new DateOnly(2026, 11, 15),
            PoliticaDeRetirada.HojeNoFuso(agora, FusoDeSaoPaulo));
    }

    [TestMethod]
    public void Periodos_usam_os_horarios_padrao_do_prd()
    {
        Assert.AreEqual(
            new TimeOnly(8, 0),
            PoliticaDeRetirada.HoraInicioPadrao(PeriodoDeRetirada.Manha));
        Assert.AreEqual(
            new TimeOnly(12, 0),
            PoliticaDeRetirada.HoraFimPadrao(PeriodoDeRetirada.Manha));
        Assert.AreEqual(
            new TimeOnly(13, 0),
            PoliticaDeRetirada.HoraInicioPadrao(PeriodoDeRetirada.Tarde));
        Assert.AreEqual(
            new TimeOnly(17, 0),
            PoliticaDeRetirada.HoraFimPadrao(PeriodoDeRetirada.Tarde));
    }
}
