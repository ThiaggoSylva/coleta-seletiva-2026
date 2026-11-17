using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Retiradas;

namespace ColetaSeletiva.Domain.Tests.Retiradas;

[TestClass]
public sealed class SolicitacaoDeRetiradaTests
{
    private static readonly Guid OrganizacaoId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CidadaoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AutorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TipoPapel = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid TipoVidro = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Data = new(2026, 11, 20);

    [TestMethod]
    public void Criacao_guarda_snapshot_normalizado_e_estado_inicial()
    {
        var solicitacao = Criar();

        Assert.AreEqual(EstadoSolicitacao.Solicitada, solicitacao.Estado);
        Assert.AreEqual("Ana Souza", solicitacao.NomeCidadao);
        Assert.AreEqual("(62) 99999-0000", solicitacao.TelefoneCidadao);
        Assert.AreEqual("GO", solicitacao.Uf);
        Assert.AreEqual("74000000", solicitacao.Cep);
        Assert.AreEqual("Rua das Flores", solicitacao.Rua);
        Assert.IsNull(solicitacao.Complemento);
        Assert.AreEqual(Agora, solicitacao.CriadoEmUtc);
        Assert.AreEqual(Agora, solicitacao.AtualizadoEmUtc);
        Assert.IsTrue(solicitacao.EstaAberta);
        Assert.IsTrue(solicitacao.PodeExibirDadosPessoais);
        Assert.HasCount(2, solicitacao.Itens);
        Assert.HasCount(1, solicitacao.Historico);
        Assert.IsNull(solicitacao.Historico[0].EstadoAnterior);
        Assert.AreEqual(EstadoSolicitacao.Solicitada, solicitacao.Historico[0].EstadoNovo);
        Assert.AreEqual(AutorId, solicitacao.Historico[0].AtorUsuarioId);
    }

    [TestMethod]
    public void Nao_permite_tipos_de_residuo_repetidos()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => Criar(itens:
            [
                (TipoPapel, 1m),
                (TipoPapel, 2m),
            ]));
    }

    [TestMethod]
    public void Rejeita_quantidade_estimada_invalida()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => Criar(itens: [(TipoPapel, 0m)]));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => Criar(itens: [(TipoPapel, -1m)]));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => Criar(itens: [(TipoPapel, 0.0001m)]));
    }

    [TestMethod]
    public void Aceita_quantidade_com_ate_tres_casas_decimais()
    {
        var solicitacao = Criar(itens: [(TipoPapel, 12.345m)]);

        Assert.AreEqual(12.345m, solicitacao.Itens[0].QuantidadeEstimada);
    }

    [TestMethod]
    public void Rejeita_uf_cep_e_endereco_invalidos()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Criar(uf: "G"));
        Assert.ThrowsExactly<ArgumentException>(() => Criar(cep: "123"));
        Assert.ThrowsExactly<ArgumentException>(() => Criar(bairro: "  "));
        Assert.ThrowsExactly<ArgumentException>(() => Criar(nome: "  "));
    }

    [TestMethod]
    public void Agendar_transiciona_e_registra_historico()
    {
        var solicitacao = Criar();

        solicitacao.Agendar(AutorId, Agora.AddHours(1));

        Assert.AreEqual(EstadoSolicitacao.Agendada, solicitacao.Estado);
        Assert.HasCount(2, solicitacao.Historico);
        Assert.AreEqual(EstadoSolicitacao.Solicitada, solicitacao.Historico[1].EstadoAnterior);
        Assert.AreEqual(EstadoSolicitacao.Agendada, solicitacao.Historico[1].EstadoNovo);
    }

    [TestMethod]
    public void Reagendar_exige_estado_agendado_e_atualiza_historico()
    {
        var solicitacao = Criar();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => solicitacao.Reagendar(Data.AddDays(1), PeriodoDeRetirada.Tarde, AutorId, Agora));

        solicitacao.Agendar(AutorId, Agora.AddHours(1));
        solicitacao.Reagendar(Data.AddDays(1), PeriodoDeRetirada.Tarde, AutorId, Agora.AddHours(2));

        Assert.AreEqual(Data.AddDays(1), solicitacao.DataLocal);
        Assert.AreEqual(PeriodoDeRetirada.Tarde, solicitacao.Periodo);
        Assert.AreEqual(EstadoSolicitacao.Agendada, solicitacao.Estado);
        CollectionAssert.AreEqual(
            new[] { "Reagendada" },
            solicitacao.Historico.Skip(2).Select(item => item.Motivo).ToArray());
    }

    [TestMethod]
    public void Reagendar_exige_periodo_valido()
    {
        var solicitacao = Criar();
        solicitacao.Agendar(AutorId, Agora.AddHours(1));

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => solicitacao.Reagendar(
                Data.AddDays(1),
                (PeriodoDeRetirada)99,
                AutorId,
                Agora.AddHours(2)));
    }

    [TestMethod]
    public void Recusar_exige_estado_solicitado_e_motivo()
    {
        var solicitacao = Criar();

        Assert.ThrowsExactly<ArgumentException>(() => solicitacao.Recusar("  ", AutorId, Agora));
        solicitacao.Recusar(" Fora da área de atendimento. ", AutorId, Agora);

        Assert.AreEqual(EstadoSolicitacao.Recusada, solicitacao.Estado);
        Assert.AreEqual("Fora da área de atendimento.", solicitacao.Motivo);
        Assert.IsFalse(solicitacao.EstaAberta);
        Assert.IsFalse(solicitacao.PodeExibirDadosPessoais);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => solicitacao.Recusar("Outro motivo", AutorId, Agora));
    }

    [TestMethod]
    public void Cancelar_somente_em_estados_abertos()
    {
        var agendada = Criar();
        agendada.Agendar(AutorId, Agora);
        agendada.Cancelar(CidadaoId, Agora.AddHours(1));

        Assert.AreEqual(EstadoSolicitacao.Cancelada, agendada.Estado);
        Assert.IsFalse(agendada.PodeExibirDadosPessoais);
        Assert.AreEqual("Cancelada pelo cidadão", agendada.Motivo);

        var coletada = Criar();
        coletada.Agendar(AutorId, Agora);
        coletada.MarcarColetada(AutorId, Agora);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => coletada.Cancelar(CidadaoId, Agora));
    }

    [TestMethod]
    public void Nao_realizada_exige_agendamento_e_motivo_fechado()
    {
        var solicitacao = Criar();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => solicitacao.MarcarNaoRealizada(
                MotivoNaoRealizacao.CidadaoAusente,
                AutorId,
                Agora));

        solicitacao.Agendar(AutorId, Agora);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => solicitacao.MarcarNaoRealizada(
                (MotivoNaoRealizacao)99,
                AutorId,
                Agora));

        solicitacao.MarcarNaoRealizada(
            MotivoNaoRealizacao.MaterialInadequado,
            AutorId,
            Agora);

        Assert.AreEqual(EstadoSolicitacao.NaoRealizada, solicitacao.Estado);
        Assert.AreEqual("MaterialInadequado", solicitacao.Motivo);
        Assert.IsFalse(solicitacao.PodeExibirDadosPessoais);
    }

    [TestMethod]
    public void Coleta_exige_agendamento_e_oculta_dados_pessoais()
    {
        var solicitacao = Criar();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => solicitacao.MarcarColetada(AutorId, Agora));

        solicitacao.Agendar(AutorId, Agora);
        solicitacao.MarcarColetada(AutorId, Agora.AddHours(1));

        Assert.AreEqual(EstadoSolicitacao.Coletada, solicitacao.Estado);
        Assert.IsFalse(solicitacao.EstaAberta);
        Assert.IsFalse(solicitacao.PodeExibirDadosPessoais);
    }

    private static SolicitacaoDeRetirada Criar(
        string nome = " Ana Souza ",
        string? telefone = "(62) 99999-0000",
        string uf = "go",
        string bairro = "Setor Bueno",
        string cep = "74000-000",
        IReadOnlyCollection<(Guid TipoResiduoId, decimal QuantidadeEstimada)>? itens = null)
    {
        return SolicitacaoDeRetirada.Criar(
            OrganizacaoId,
            CidadaoId,
            nome,
            telefone,
            uf,
            5208707,
            "Goiânia",
            bairro,
            "Rua das Flores",
            "123",
            "   ",
            cep,
            null,
            "  ",
            Data,
            PeriodoDeRetirada.Manha,
            itens ?? [(TipoPapel, 2m), (TipoVidro, 1.5m)],
            AutorId,
            Agora);
    }
}
