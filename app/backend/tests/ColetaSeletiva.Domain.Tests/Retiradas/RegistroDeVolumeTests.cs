using ColetaSeletiva.Domain.Retiradas;

namespace ColetaSeletiva.Domain.Tests.Retiradas;

[TestClass]
public sealed class RegistroDeVolumeTests
{
    private static readonly Guid OrganizacaoId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TipoResiduoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SolicitacaoId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid PontoId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid OriginalId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid OperadorId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly DataLocal = new(2026, 11, 15);

    [TestMethod]
    public void Volume_de_retirada_exige_quantidade_positiva_e_referencia()
    {
        var volume = RegistroDeVolume.CriarRetirada(
            OrganizacaoId,
            TipoResiduoId,
            12.345m,
            SolicitacaoId,
            OperadorId,
            " Operador ",
            Agora,
            DataLocal);

        Assert.AreEqual(OrigemDoVolume.Retirada, volume.Origem);
        Assert.AreEqual(12.345m, volume.Quantidade);
        Assert.AreEqual("Operador", volume.OperadorNome);
        Assert.AreEqual(SolicitacaoId, volume.SolicitacaoId);
        Assert.IsNull(volume.PontoId);
        Assert.IsFalse(volume.EhAjuste);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => RegistroDeVolume.CriarRetirada(
                OrganizacaoId,
                TipoResiduoId,
                0m,
                SolicitacaoId,
                OperadorId,
                "Operador",
                Agora,
                DataLocal));
        Assert.ThrowsExactly<ArgumentException>(
            () => RegistroDeVolume.CriarRetirada(
                OrganizacaoId,
                TipoResiduoId,
                1m,
                Guid.Empty,
                OperadorId,
                "Operador",
                Agora,
                DataLocal));
    }

    [TestMethod]
    public void Recebimento_em_ponto_exige_ponto()
    {
        var volume = RegistroDeVolume.CriarRecebimento(
            OrganizacaoId,
            TipoResiduoId,
            4m,
            PontoId,
            OperadorId,
            "Operador",
            Agora,
            DataLocal);

        Assert.AreEqual(OrigemDoVolume.RecebimentoEmPonto, volume.Origem);
        Assert.AreEqual(PontoId, volume.PontoId);

        Assert.ThrowsExactly<ArgumentException>(
            () => RegistroDeVolume.CriarRecebimento(
                OrganizacaoId,
                TipoResiduoId,
                4m,
                Guid.Empty,
                OperadorId,
                "Operador",
                Agora,
                DataLocal));
    }

    [TestMethod]
    public void Ajuste_aceita_quantidade_negativa_mas_nao_zero()
    {
        var ajuste = RegistroDeVolume.CriarAjuste(
            OrganizacaoId,
            TipoResiduoId,
            -2.5m,
            OriginalId,
            OperadorId,
            "Operador",
            Agora,
            DataLocal,
            " Correção de pesagem ");

        Assert.AreEqual(OrigemDoVolume.Ajuste, ajuste.Origem);
        Assert.AreEqual(-2.5m, ajuste.Quantidade);
        Assert.AreEqual("Correção de pesagem", ajuste.Motivo);
        Assert.IsTrue(ajuste.EhAjuste);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => RegistroDeVolume.CriarAjuste(
                OrganizacaoId,
                TipoResiduoId,
                0m,
                OriginalId,
                OperadorId,
                "Operador",
                Agora,
                DataLocal,
                "Correção"));
    }

    [TestMethod]
    public void Ajuste_exige_registro_original_e_motivo_valido()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => RegistroDeVolume.CriarAjuste(
                OrganizacaoId,
                TipoResiduoId,
                1m,
                Guid.Empty,
                OperadorId,
                "Operador",
                Agora,
                DataLocal,
                "Correção"));
        Assert.ThrowsExactly<ArgumentException>(
            () => RegistroDeVolume.CriarAjuste(
                OrganizacaoId,
                TipoResiduoId,
                1m,
                OriginalId,
                OperadorId,
                "Operador",
                Agora,
                DataLocal,
                "   "));
        Assert.ThrowsExactly<ArgumentException>(
            () => RegistroDeVolume.CriarAjuste(
                OrganizacaoId,
                TipoResiduoId,
                1m,
                OriginalId,
                OperadorId,
                "Operador",
                Agora,
                DataLocal,
                new string('a', RegistroDeVolume.MotivoMaxLength + 1)));
    }

    [TestMethod]
    public void Registros_exigem_organizacao_tipo_e_operador()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => RegistroDeVolume.CriarRetirada(
                Guid.Empty,
                TipoResiduoId,
                1m,
                SolicitacaoId,
                OperadorId,
                "Operador",
                Agora,
                DataLocal));
        Assert.ThrowsExactly<ArgumentException>(
            () => RegistroDeVolume.CriarRetirada(
                OrganizacaoId,
                Guid.Empty,
                1m,
                SolicitacaoId,
                OperadorId,
                "Operador",
                Agora,
                DataLocal));
        Assert.ThrowsExactly<ArgumentException>(
            () => RegistroDeVolume.CriarRetirada(
                OrganizacaoId,
                TipoResiduoId,
                1m,
                SolicitacaoId,
                Guid.Empty,
                "Operador",
                Agora,
                DataLocal));
        Assert.ThrowsExactly<ArgumentException>(
            () => RegistroDeVolume.CriarRetirada(
                OrganizacaoId,
                TipoResiduoId,
                1m,
                SolicitacaoId,
                OperadorId,
                "  ",
                Agora,
                DataLocal));
    }
}
