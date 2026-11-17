using ColetaSeletiva.Domain.Entidades;

namespace ColetaSeletiva.Domain.Tests.Entidades;

[TestClass]
public sealed class BloqueioDeDataTests
{
    private static readonly Guid UsuarioId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Criacao_normaliza_motivo_opcional()
    {
        var bloqueio = BloqueioDeData.Criar(
            new DateOnly(2026, 11, 20),
            "  Feriado municipal  ",
            UsuarioId,
            Agora);

        Assert.AreEqual("Feriado municipal", bloqueio.Motivo);
        Assert.AreEqual(UsuarioId, bloqueio.CriadoPorUsuarioId);
        Assert.AreEqual(Agora, bloqueio.CriadoEmUtc);

        var semMotivo = BloqueioDeData.Criar(
            new DateOnly(2026, 11, 21),
            "   ",
            UsuarioId,
            Agora);

        Assert.IsNull(semMotivo.Motivo);
    }

    [TestMethod]
    public void Exige_autor_e_limita_o_tamanho_do_motivo()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => BloqueioDeData.Criar(
                new DateOnly(2026, 11, 20),
                null,
                Guid.Empty,
                Agora));
        Assert.ThrowsExactly<ArgumentException>(
            () => BloqueioDeData.Criar(
                new DateOnly(2026, 11, 20),
                new string('a', BloqueioDeData.MotivoMaxLength + 1),
                UsuarioId,
                Agora));
    }
}
