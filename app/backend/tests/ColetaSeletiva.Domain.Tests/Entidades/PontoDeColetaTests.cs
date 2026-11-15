using ColetaSeletiva.Domain.Entidades;

namespace ColetaSeletiva.Domain.Tests.Entidades;

[TestClass]
public sealed class PontoDeColetaTests
{
    private static readonly Guid TipoPapel = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo FusoDeSaoPaulo =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    [TestMethod]
    public void Ponto_ativo_abre_nos_limites_do_horario_no_fuso_da_organizacao()
    {
        var ponto = CriarPonto();

        // 2026-11-16 é segunda-feira; São Paulo está em UTC-3.
        Assert.IsFalse(ponto.EstaAberto(Instante(10, 59), FusoDeSaoPaulo));
        Assert.IsTrue(ponto.EstaAberto(Instante(11, 0), FusoDeSaoPaulo));
        Assert.IsTrue(ponto.EstaAberto(Instante(19, 59), FusoDeSaoPaulo));
        Assert.IsFalse(ponto.EstaAberto(Instante(20, 0), FusoDeSaoPaulo));
    }

    [TestMethod]
    public void Ponto_fora_do_dia_de_funcionamento_nao_esta_aberto()
    {
        var ponto = CriarPonto();
        var sabado = new DateTimeOffset(2026, 11, 14, 14, 0, 0, TimeSpan.Zero);

        Assert.IsFalse(ponto.EstaAberto(sabado, FusoDeSaoPaulo));
    }

    [TestMethod]
    public void Ponto_indisponivel_ou_retirado_nunca_esta_aberto()
    {
        var indisponivel = CriarPonto(status: StatusPonto.IndisponivelTemporariamente);
        var retirado = CriarPonto();
        retirado.Retirar(Agora);

        Assert.IsFalse(indisponivel.EstaAberto(Instante(12, 0), FusoDeSaoPaulo));
        Assert.IsFalse(retirado.EstaAberto(Instante(12, 0), FusoDeSaoPaulo));
    }

    [TestMethod]
    public void Horario_que_atravessa_a_meia_noite_e_rejeitado()
    {
        var horarios = new[]
        {
            new HorarioDeFuncionamento(DayOfWeek.Monday, new TimeOnly(22, 0), new TimeOnly(2, 0)),
        };

        Assert.ThrowsExactly<ArgumentException>(() => CriarPonto(horarios: horarios));
    }

    [TestMethod]
    public void Ponto_exige_ao_menos_um_tipo_aceito()
    {
        Assert.ThrowsExactly<ArgumentException>(() => CriarPonto(tipos: []));
    }

    [TestMethod]
    public void Coordenadas_fora_do_intervalo_sao_rejeitadas()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => CriarPonto(latitude: 120));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => CriarPonto(longitude: -200));
    }

    [TestMethod]
    public void Retirada_logica_preserva_dados_e_e_idempotente()
    {
        var ponto = CriarPonto();

        ponto.Retirar(Agora);
        ponto.Retirar(Agora.AddDays(1));

        Assert.IsNotNull(ponto.ExcluidoEmUtc);
        Assert.AreEqual(Agora, ponto.ExcluidoEmUtc);
        Assert.AreEqual("Ponto Centro", ponto.Nome);
        Assert.ThrowsExactly<InvalidOperationException>(
            () => ponto.AlterarStatus(StatusPonto.Ativo, Agora.AddDays(2)));
    }

    [TestMethod]
    public void Cep_e_normalizado_para_oito_digitos()
    {
        var ponto = CriarPonto(cep: "01310-100");

        Assert.AreEqual("01310100", ponto.Cep);
    }

    private static DateTimeOffset Instante(int hora, int minuto)
    {
        return new DateTimeOffset(2026, 11, 16, hora, minuto, 0, TimeSpan.Zero);
    }

    private static PontoDeColeta CriarPonto(
        StatusPonto status = StatusPonto.Ativo,
        IReadOnlyCollection<HorarioDeFuncionamento>? horarios = null,
        IReadOnlyCollection<Guid>? tipos = null,
        double latitude = -23.5613,
        double longitude = -46.6558,
        string cep = "01310100")
    {
        return PontoDeColeta.Criar(
            nome: "Ponto Centro",
            municipioId: 3550308,
            uf: "SP",
            bairro: "Bela Vista",
            rua: "Avenida Paulista",
            numero: "1000",
            complemento: null,
            cep: cep,
            latitude: latitude,
            longitude: longitude,
            observacoes: null,
            status: status,
            horarios: horarios
                ?? [new HorarioDeFuncionamento(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0))],
            tiposResiduoIds: tipos ?? [TipoPapel],
            agora: Agora);
    }
}
