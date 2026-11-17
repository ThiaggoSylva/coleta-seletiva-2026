using ColetaSeletiva.Domain.Entidades;

namespace ColetaSeletiva.Domain.Tests.Entidades;

[TestClass]
public sealed class DisponibilidadeDeRetiradaTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Aceita_capacidade_de_zero_ate_o_maximo()
    {
        var desativada = DisponibilidadeDeRetirada.Criar(
            DayOfWeek.Monday,
            PeriodoDeRetirada.Manha,
            0,
            Agora);
        var cheia = DisponibilidadeDeRetirada.Criar(
            DayOfWeek.Friday,
            PeriodoDeRetirada.Tarde,
            DisponibilidadeDeRetirada.CapacidadeMaxima,
            Agora);

        Assert.AreEqual(0, desativada.Capacidade);
        Assert.AreEqual(DisponibilidadeDeRetirada.CapacidadeMaxima, cheia.Capacidade);
        Assert.AreEqual(Agora, desativada.CriadoEmUtc);
        Assert.AreEqual(Agora, desativada.AtualizadoEmUtc);
    }

    [TestMethod]
    public void Rejeita_capacidade_negativa_ou_acima_do_maximo()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => DisponibilidadeDeRetirada.Criar(
                DayOfWeek.Monday,
                PeriodoDeRetirada.Manha,
                -1,
                Agora));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => DisponibilidadeDeRetirada.Criar(
                DayOfWeek.Monday,
                PeriodoDeRetirada.Manha,
                DisponibilidadeDeRetirada.CapacidadeMaxima + 1,
                Agora));
    }

    [TestMethod]
    public void Rejeita_dia_da_semana_e_periodo_invalidos()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => DisponibilidadeDeRetirada.Criar(
                (DayOfWeek)9,
                PeriodoDeRetirada.Manha,
                1,
                Agora));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => DisponibilidadeDeRetirada.Criar(
                DayOfWeek.Monday,
                (PeriodoDeRetirada)9,
                1,
                Agora));
    }

    [TestMethod]
    public void Definir_capacidade_atualiza_valor_e_instante()
    {
        var disponibilidade = DisponibilidadeDeRetirada.Criar(
            DayOfWeek.Wednesday,
            PeriodoDeRetirada.Manha,
            5,
            Agora);

        disponibilidade.DefinirCapacidade(8, Agora.AddDays(1));

        Assert.AreEqual(8, disponibilidade.Capacidade);
        Assert.AreEqual(Agora.AddDays(1), disponibilidade.AtualizadoEmUtc);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => disponibilidade.DefinirCapacidade(-2, Agora.AddDays(2)));
    }
}
