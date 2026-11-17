using ColetaSeletiva.Domain.Entidades;

namespace ColetaSeletiva.Domain.Tests.Entidades;

[TestClass]
public sealed class NotificacaoTests
{
    private static readonly Guid UsuarioId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrganizacaoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ReferenciaId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Criacao_normaliza_texto_e_inicia_nao_lida()
    {
        var notificacao = Notificacao.Criar(
            UsuarioId,
            OrganizacaoId,
            TipoDeNotificacao.SolicitacaoCriada,
            ReferenciaId,
            "  Nova solicitação  ",
            "  Há uma nova retirada aguardando análise.  ",
            Agora);

        Assert.AreEqual("Nova solicitação", notificacao.Titulo);
        Assert.AreEqual("Há uma nova retirada aguardando análise.", notificacao.Mensagem);
        Assert.IsFalse(notificacao.Lida);
        Assert.IsNull(notificacao.LidaEmUtc);
        Assert.AreEqual(Agora, notificacao.CriadaEmUtc);
    }

    [TestMethod]
    public void Rejeita_destinatario_vazio_tipo_invalido_e_textos_vazios()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => Notificacao.Criar(
                Guid.Empty,
                OrganizacaoId,
                TipoDeNotificacao.SolicitacaoCriada,
                ReferenciaId,
                "Título",
                "Mensagem",
                Agora));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => Notificacao.Criar(
                UsuarioId,
                OrganizacaoId,
                (TipoDeNotificacao)99,
                ReferenciaId,
                "Título",
                "Mensagem",
                Agora));
        Assert.ThrowsExactly<ArgumentException>(
            () => Notificacao.Criar(
                UsuarioId,
                OrganizacaoId,
                TipoDeNotificacao.SolicitacaoCriada,
                ReferenciaId,
                "  ",
                "Mensagem",
                Agora));
        Assert.ThrowsExactly<ArgumentException>(
            () => Notificacao.Criar(
                UsuarioId,
                OrganizacaoId,
                TipoDeNotificacao.SolicitacaoCriada,
                ReferenciaId,
                "Título",
                "  ",
                Agora));
    }

    [TestMethod]
    public void Rejeita_titulo_e_mensagem_acima_do_limite()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => Notificacao.Criar(
                UsuarioId,
                OrganizacaoId,
                TipoDeNotificacao.SolicitacaoCriada,
                ReferenciaId,
                new string('a', Notificacao.TituloMaxLength + 1),
                "Mensagem",
                Agora));
        Assert.ThrowsExactly<ArgumentException>(
            () => Notificacao.Criar(
                UsuarioId,
                OrganizacaoId,
                TipoDeNotificacao.SolicitacaoCriada,
                ReferenciaId,
                "Título",
                new string('a', Notificacao.MensagemMaxLength + 1),
                Agora));
    }

    [TestMethod]
    public void Marcar_lida_registra_a_data_e_e_idempotente()
    {
        var notificacao = Notificacao.Criar(
            UsuarioId,
            OrganizacaoId,
            TipoDeNotificacao.RetiradaAgendada,
            ReferenciaId,
            "Retirada agendada",
            "A retirada foi agendada.",
            Agora);

        notificacao.MarcarLida(Agora.AddHours(1));
        var primeiraLeitura = notificacao.LidaEmUtc;
        notificacao.MarcarLida(Agora.AddHours(2));

        Assert.IsTrue(notificacao.Lida);
        Assert.AreEqual(Agora.AddHours(1), primeiraLeitura);
        Assert.AreEqual(primeiraLeitura, notificacao.LidaEmUtc);
    }
}
