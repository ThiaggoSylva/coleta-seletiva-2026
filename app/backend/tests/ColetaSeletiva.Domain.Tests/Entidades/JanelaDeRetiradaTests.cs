using ColetaSeletiva.Domain.Entidades;

namespace ColetaSeletiva.Domain.Tests.Entidades;

[TestClass]
public sealed class JanelaDeRetiradaTests
{
    private static readonly Guid OrganizacaoId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Criacao_guarda_data_periodo_e_instante()
    {
        var janela = JanelaDeRetirada.Criar(
            OrganizacaoId,
            new DateOnly(2026, 11, 20),
            PeriodoDeRetirada.Tarde,
            Agora);

        Assert.AreEqual(OrganizacaoId, janela.OrganizacaoId);
        Assert.AreEqual(new DateOnly(2026, 11, 20), janela.DataLocal);
        Assert.AreEqual(PeriodoDeRetirada.Tarde, janela.Periodo);
        Assert.AreEqual(Agora, janela.CriadoEmUtc);
    }

    [TestMethod]
    public void Exige_organizacao_e_periodo_valido()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => JanelaDeRetirada.Criar(
                Guid.Empty,
                new DateOnly(2026, 11, 20),
                PeriodoDeRetirada.Manha,
                Agora));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => JanelaDeRetirada.Criar(
                OrganizacaoId,
                new DateOnly(2026, 11, 20),
                (PeriodoDeRetirada)9,
                Agora));
    }
}
