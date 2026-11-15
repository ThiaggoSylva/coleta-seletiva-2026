using ColetaSeletiva.Application.Acesso.Sessao;
using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Seguranca;
using ColetaSeletiva.Domain.Identidade;
using FluentResults;
using Moq;

namespace ColetaSeletiva.Application.Tests.Acesso;

[TestClass]
public sealed class AlterarSenhaHandlerTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 12, 15, 0, 0, TimeSpan.Zero);
    private static readonly Guid CorrelacaoId = Guid.Parse("12121212-3434-5656-7878-909090909090");

    [TestMethod]
    public async Task Troca_valida_revoga_todas_as_sessoes_e_registra_auditoria()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandler().ExecutarAsync(EntradaValida(), CorrelacaoId);

        Assert.IsTrue(resultado.IsSuccess, Descrever(resultado));

        cenario.Alteracao.Verify(
            servico => servico.AlterarAsync(
                cenario.UsuarioId,
                "SenhaAtual!123",
                "NovaSenha!456",
                It.IsAny<CancellationToken>()),
            Times.Once);
        cenario.RefreshTokens.Verify(
            tokens => tokens.RevogarTodasAsync(
                cenario.UsuarioId,
                Agora,
                It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.IsNotNull(cenario.EventoRegistrado);
        Assert.AreEqual(cenario.OrganizacaoId, cenario.EventoRegistrado.OrganizacaoId);
        Assert.AreEqual(cenario.UsuarioId, cenario.EventoRegistrado.AtorUsuarioId);
        Assert.AreEqual(AcoesAuditadas.SenhaAlterada, cenario.EventoRegistrado.Acao);
        Assert.AreEqual(EntidadesAuditadas.Usuario, cenario.EventoRegistrado.Entidade);
        Assert.AreEqual(cenario.UsuarioId, cenario.EventoRegistrado.EntidadeId);
        Assert.AreEqual(CorrelacaoId, cenario.EventoRegistrado.CorrelacaoId);
        Assert.AreEqual(Agora, cenario.EventoRegistrado.OcorridoEmUtc);

        cenario.Transacao.Verify(
            transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        cenario.Transacao.Verify(
            transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Senha_atual_incorreta_desfaz_a_transacao_sem_revogar_sessoes()
    {
        var cenario = new Cenario(
            alteracao: Result.Fail(
                new ErroDeValidacao(
                    nameof(AlterarSenhaEntrada.SenhaAtual),
                    "A senha atual informada está incorreta.")));

        var resultado = await cenario.CriarHandler().ExecutarAsync(EntradaValida(), CorrelacaoId);

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(
            "A senha atual informada está incorreta.",
            resultado.Errors.OfType<ErroDeValidacao>().Single().Message);

        cenario.RefreshTokens.Verify(
            tokens => tokens.RevogarTodasAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.Auditoria.Verify(
            auditoria => auditoria.RegistrarAsync(
                It.IsAny<EventoDeAuditoria>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.Transacao.Verify(
            transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.Transacao.Verify(
            transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Nova_senha_fora_da_politica_retorna_erro_sem_alterar_a_senha()
    {
        var cenario = new Cenario(
            alteracao: Result.Fail(
                new ErroDeValidacao(
                    "NovaSenha",
                    "A nova senha deve ter pelo menos 12 caracteres.")));

        var resultado = await cenario.CriarHandler().ExecutarAsync(
            new AlterarSenhaEntrada("SenhaAtual!123", "curta"),
            CorrelacaoId);

        Assert.IsTrue(resultado.IsFailed);
        var erro = resultado.Errors.OfType<ErroDeValidacao>().Single();
        Assert.AreEqual("NovaSenha", erro.Campo);
        StringAssert.Contains(erro.Message, "12");

        cenario.RefreshTokens.Verify(
            tokens => tokens.RevogarTodasAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.Transacao.Verify(
            transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Entrada_invalida_nao_inicia_transacao_nem_altera_a_senha()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandler().ExecutarAsync(
            new AlterarSenhaEntrada(string.Empty, string.Empty),
            CorrelacaoId);

        Assert.IsTrue(resultado.IsFailed);
        var campos = resultado.Errors
            .OfType<ErroDeValidacao>()
            .Select(erro => erro.Campo)
            .ToList();

        CollectionAssert.Contains(campos, nameof(AlterarSenhaEntrada.SenhaAtual));
        CollectionAssert.Contains(campos, nameof(AlterarSenhaEntrada.NovaSenha));

        cenario.Alteracao.Verify(
            servico => servico.AlterarAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Sessao_sem_escopo_nao_altera_a_senha()
    {
        var cenario = new Cenario(autenticado: false);

        var resultado = await cenario.CriarHandler().ExecutarAsync(EntradaValida(), CorrelacaoId);

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroNaoAutenticado>(resultado.Errors.Single());

        cenario.Alteracao.Verify(
            servico => servico.AlterarAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static AlterarSenhaEntrada EntradaValida()
    {
        return new AlterarSenhaEntrada("SenhaAtual!123", "NovaSenha!456");
    }

    private static string Descrever(ResultBase resultado)
    {
        return string.Join(
            "; ",
            resultado.Errors.Select(erro =>
                erro.Metadata.TryGetValue("codigo", out var codigo)
                    ? $"{codigo}: {erro.Message}"
                    : erro.Message));
    }

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return Agora;
        }
    }

    private sealed class Cenario
    {
        public Cenario(Result? alteracao = null, bool autenticado = true)
        {
            UsuarioId = Guid.NewGuid();
            OrganizacaoId = Guid.NewGuid();

            TenantContext
                .SetupGet(contexto => contexto.Autenticado)
                .Returns(autenticado);
            TenantContext
                .SetupGet(contexto => contexto.EscopoValido)
                .Returns(autenticado);
            TenantContext
                .SetupGet(contexto => contexto.UsuarioId)
                .Returns(autenticado ? UsuarioId : null);
            TenantContext
                .SetupGet(contexto => contexto.OrganizacaoId)
                .Returns(autenticado ? OrganizacaoId : null);

            Alteracao
                .Setup(servico => servico.AlterarAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(alteracao ?? Result.Ok());

            RefreshTokens
                .Setup(tokens => tokens.RevogarTodasAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Auditoria
                .Setup(auditoria => auditoria.RegistrarAsync(
                    It.IsAny<EventoDeAuditoria>(),
                    It.IsAny<CancellationToken>()))
                .Callback<EventoDeAuditoria, CancellationToken>((evento, _) =>
                    EventoRegistrado = evento)
                .Returns(Task.CompletedTask);

            UnidadeDeTrabalho
                .Setup(unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Transacao.Object);

            Transacao
                .Setup(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Transacao
                .Setup(transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public Guid UsuarioId { get; }

        public Guid OrganizacaoId { get; }

        public Mock<ITenantContext> TenantContext { get; } = new();

        public Mock<IAlteracaoDeSenha> Alteracao { get; } = new();

        public Mock<IRepositorioDeRefreshTokens> RefreshTokens { get; } = new();

        public Mock<IRegistroDeAuditoria> Auditoria { get; } = new();

        public Mock<IUnidadeDeTrabalho> UnidadeDeTrabalho { get; } = new();

        public Mock<ITransacao> Transacao { get; } = new();

        public EventoDeAuditoria? EventoRegistrado { get; private set; }

        public AlterarSenhaHandler CriarHandler()
        {
            return new AlterarSenhaHandler(
                new AlterarSenhaValidator(),
                TenantContext.Object,
                Alteracao.Object,
                RefreshTokens.Object,
                Auditoria.Object,
                UnidadeDeTrabalho.Object,
                new RelogioFixo());
        }
    }
}
