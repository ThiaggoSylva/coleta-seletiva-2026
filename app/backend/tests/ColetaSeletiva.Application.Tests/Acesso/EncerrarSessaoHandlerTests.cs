using ColetaSeletiva.Application.Acesso.Sessao;
using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Identidade;
using FluentResults;
using Moq;

namespace ColetaSeletiva.Application.Tests.Acesso;

[TestClass]
public sealed class EncerrarSessaoHandlerTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid CorrelacaoId = Guid.Parse("99999999-8888-7777-6666-555555555555");
    private const string RefreshValido = "refresh-valido";

    [TestMethod]
    public async Task Logout_revoga_a_familia_e_registra_auditoria()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandler().ExecutarAsync(RefreshValido, CorrelacaoId);

        Assert.IsTrue(resultado.IsSuccess);

        cenario.RefreshTokens.Verify(
            tokens => tokens.RevogarFamiliaAsync(
                cenario.Token.UsuarioId,
                cenario.Token.FamiliaId,
                Agora,
                It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.IsNotNull(cenario.EventoRegistrado);
        Assert.AreEqual(cenario.Conta.OrganizacaoId, cenario.EventoRegistrado.OrganizacaoId);
        Assert.AreEqual(cenario.Token.UsuarioId, cenario.EventoRegistrado.AtorUsuarioId);
        Assert.AreEqual(AcoesAuditadas.SessaoEncerrada, cenario.EventoRegistrado.Acao);
        Assert.AreEqual(EntidadesAuditadas.Usuario, cenario.EventoRegistrado.Entidade);
        Assert.AreEqual(cenario.Token.UsuarioId, cenario.EventoRegistrado.EntidadeId);
        Assert.AreEqual(CorrelacaoId, cenario.EventoRegistrado.CorrelacaoId);
        Assert.AreEqual(Agora, cenario.EventoRegistrado.OcorridoEmUtc);

        cenario.Transacao.Verify(
            transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Logout_sem_cookie_ou_com_token_desconhecido_e_idempotente()
    {
        var semCookie = new Cenario();
        var desconhecido = new Cenario(semToken: true);

        var resultadoSemCookie = await semCookie.CriarHandler().ExecutarAsync(null, CorrelacaoId);
        var resultadoDesconhecido = await desconhecido.CriarHandler().ExecutarAsync(
            RefreshValido,
            CorrelacaoId);

        Assert.IsTrue(resultadoSemCookie.IsSuccess);
        Assert.IsTrue(resultadoDesconhecido.IsSuccess);
        semCookie.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        semCookie.RefreshTokens.Verify(
            tokens => tokens.ObterPorHashAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        desconhecido.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        desconhecido.Auditoria.Verify(
            auditoria => auditoria.RegistrarAsync(
                It.IsAny<EventoDeAuditoria>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Logout_de_familia_ja_revogada_nao_repete_a_revogacao()
    {
        var token = CriarToken();
        token.RevogadoEmUtc = Agora.AddMinutes(-1);
        var cenario = new Cenario(token);

        var resultado = await cenario.CriarHandler().ExecutarAsync(RefreshValido, CorrelacaoId);

        Assert.IsTrue(resultado.IsSuccess);
        cenario.RefreshTokens.Verify(
            tokens => tokens.RevogarFamiliaAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Logout_revoga_mesmo_quando_a_conta_nao_pode_ser_carregada()
    {
        var cenario = new Cenario(
            autenticacao: Result.Fail<ContaAutenticada>(
                new ErroNaoAutenticado(MensagensDeSessao.SessaoInvalida)));

        var resultado = await cenario.CriarHandler().ExecutarAsync(RefreshValido, CorrelacaoId);

        Assert.IsTrue(resultado.IsSuccess);
        cenario.RefreshTokens.Verify(
            tokens => tokens.RevogarFamiliaAsync(
                cenario.Token.UsuarioId,
                cenario.Token.FamiliaId,
                Agora,
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.IsNotNull(cenario.EventoRegistrado);
        Assert.IsNull(cenario.EventoRegistrado.OrganizacaoId);
    }

    private static RefreshToken CriarToken()
    {
        return new RefreshToken
        {
            UsuarioId = Guid.NewGuid(),
            FamiliaId = Guid.NewGuid(),
            Hash = RefreshTokenOpaco.CalcularHash(RefreshValido),
            CriadoEmUtc = Agora.AddHours(-1),
            ExpiraEmUtc = Agora.AddDays(6),
        };
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
        public Cenario(
            RefreshToken? token = null,
            Result<ContaAutenticada>? autenticacao = null,
            bool semToken = false)
        {
            Token = token ?? CriarToken();
            var tokenParaConsulta = semToken ? null : Token;
            Conta = new ContaAutenticada(
                Id: Token.UsuarioId,
                Nome: "Administrador de Teste",
                Email: "admin.teste@example.org",
                Papel: Papeis.Administrador,
                OrganizacaoId: Guid.NewGuid(),
                TrocaSenhaPendente: false,
                Ativa: true);

            RefreshTokens
                .Setup(tokens => tokens.ObterPorHashAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(tokenParaConsulta);

            Autenticacao
                .Setup(servico => servico.ObterPorUsuarioIdAsync(
                    Token.UsuarioId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(autenticacao ?? Result.Ok(Conta));

            RefreshTokens
                .Setup(tokens => tokens.RevogarFamiliaAsync(
                    It.IsAny<Guid>(),
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

        public RefreshToken Token { get; }

        public ContaAutenticada Conta { get; }

        public Mock<IAutenticacaoDeContas> Autenticacao { get; } = new();

        public Mock<IRepositorioDeRefreshTokens> RefreshTokens { get; } = new();

        public Mock<IRegistroDeAuditoria> Auditoria { get; } = new();

        public Mock<IUnidadeDeTrabalho> UnidadeDeTrabalho { get; } = new();

        public Mock<ITransacao> Transacao { get; } = new();

        public EventoDeAuditoria? EventoRegistrado { get; private set; }

        public EncerrarSessaoHandler CriarHandler()
        {
            return new EncerrarSessaoHandler(
                Autenticacao.Object,
                RefreshTokens.Object,
                Auditoria.Object,
                UnidadeDeTrabalho.Object,
                new RelogioFixo());
        }
    }
}
