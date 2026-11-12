using ColetaSeletiva.Application.Acesso.Sessao;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Identidade;
using FluentResults;
using Moq;

namespace ColetaSeletiva.Application.Tests.Acesso;

[TestClass]
public sealed class RenovarSessaoHandlerTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);
    private const string RefreshValido = "refresh-valido";

    [TestMethod]
    public async Task Refresh_valido_consome_o_token_e_rotaciona_na_mesma_familia()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandler().ExecutarAsync(RefreshValido);

        Assert.IsTrue(resultado.IsSuccess, Descrever(resultado));
        Assert.AreEqual("token-de-acesso", resultado.Value.TokenDeAcesso);
        Assert.AreEqual(Agora.AddMinutes(15), resultado.Value.ExpiraEmUtc);
        Assert.AreNotEqual(RefreshValido, resultado.Value.RefreshToken);

        Assert.IsNotNull(cenario.RefreshAdicionado);
        Assert.AreEqual(cenario.Token.UsuarioId, cenario.RefreshAdicionado.UsuarioId);
        Assert.AreEqual(cenario.Token.FamiliaId, cenario.RefreshAdicionado.FamiliaId);
        Assert.AreEqual(Agora, cenario.RefreshAdicionado.CriadoEmUtc);
        Assert.AreEqual(Agora.AddDays(7), cenario.RefreshAdicionado.ExpiraEmUtc);
        Assert.AreEqual(
            RefreshTokenOpaco.CalcularHash(resultado.Value.RefreshToken),
            cenario.RefreshAdicionado.Hash);
        Assert.AreNotEqual(resultado.Value.RefreshToken, cenario.RefreshAdicionado.Hash);

        cenario.RefreshTokens.Verify(
            tokens => tokens.ConsumirAsync(
                cenario.Token.Id,
                cenario.Token.UsuarioId,
                Agora,
                It.IsAny<CancellationToken>()),
            Times.Once);
        cenario.RefreshTokens.Verify(
            tokens => tokens.RevogarFamiliaAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.Transacao.Verify(
            transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Refresh_ja_consumido_revoga_a_familia_e_falha_com_mensagem_generica()
    {
        var token = CriarToken(consumidoEmUtc: Agora.AddMinutes(-5));
        var cenario = new Cenario(token);

        var resultado = await cenario.CriarHandler().ExecutarAsync(RefreshValido);

        Assert.IsTrue(resultado.IsFailed);
        var erro = resultado.Errors.OfType<ErroNaoAutenticado>().Single();
        Assert.AreEqual(MensagensDeSessao.SessaoInvalida, erro.Message);

        cenario.RefreshTokens.Verify(
            tokens => tokens.RevogarFamiliaAsync(
                token.UsuarioId,
                token.FamiliaId,
                Agora,
                It.IsAny<CancellationToken>()),
            Times.Once);
        cenario.RefreshTokens.Verify(
            tokens => tokens.ConsumirAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.RefreshTokens.Verify(
            tokens => tokens.AdicionarAsync(
                It.IsAny<RefreshToken>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.Gerador.Verify(
            gerador => gerador.Gerar(It.IsAny<ContaAutenticada>(), It.IsAny<DateTimeOffset>()),
            Times.Never);
        cenario.Transacao.Verify(
            transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Refresh_expirado_falha_sem_consumir_nem_revogar()
    {
        var token = CriarToken(expiraEmUtc: Agora.AddSeconds(-1));
        var cenario = new Cenario(token);

        var resultado = await cenario.CriarHandler().ExecutarAsync(RefreshValido);

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroNaoAutenticado>(resultado.Errors.Single());

        cenario.RefreshTokens.Verify(
            tokens => tokens.ConsumirAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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
    public async Task Conta_inativa_revoga_a_familia_e_nao_renova()
    {
        var token = CriarToken();
        var cenario = new Cenario(token, conta: NovaConta() with { Ativa = false });

        var resultado = await cenario.CriarHandler().ExecutarAsync(RefreshValido);

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroNaoAutenticado>(resultado.Errors.Single());

        cenario.RefreshTokens.Verify(
            tokens => tokens.RevogarFamiliaAsync(
                token.UsuarioId,
                token.FamiliaId,
                Agora,
                It.IsAny<CancellationToken>()),
            Times.Once);
        cenario.RefreshTokens.Verify(
            tokens => tokens.ConsumirAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.Transacao.Verify(
            transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Consumo_concorrente_revoga_a_familia_sem_emitir_novo_refresh()
    {
        var token = CriarToken();
        var cenario = new Cenario(token, consumoBemSucedido: false);

        var resultado = await cenario.CriarHandler().ExecutarAsync(RefreshValido);

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroNaoAutenticado>(resultado.Errors.Single());

        cenario.RefreshTokens.Verify(
            tokens => tokens.RevogarFamiliaAsync(
                token.UsuarioId,
                token.FamiliaId,
                Agora,
                It.IsAny<CancellationToken>()),
            Times.Once);
        cenario.RefreshTokens.Verify(
            tokens => tokens.AdicionarAsync(
                It.IsAny<RefreshToken>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.Transacao.Verify(
            transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Refresh_desconhecido_falha_sem_iniciar_transacao()
    {
        var cenario = new Cenario(semToken: true);

        var resultado = await cenario.CriarHandler().ExecutarAsync(RefreshValido);

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroNaoAutenticado>(resultado.Errors.Single());
        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Refresh_ausente_ou_longo_demais_falha_sem_consultar_o_repositorio()
    {
        var cenario = new Cenario();

        var ausente = await cenario.CriarHandler().ExecutarAsync(" ");
        var longo = await cenario.CriarHandler().ExecutarAsync(new string('a', 300));

        Assert.IsTrue(ausente.IsFailed);
        Assert.IsTrue(longo.IsFailed);
        cenario.RefreshTokens.Verify(
            tokens => tokens.ObterPorHashAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static RefreshToken CriarToken(
        DateTimeOffset? consumidoEmUtc = null,
        DateTimeOffset? expiraEmUtc = null)
    {
        return new RefreshToken
        {
            UsuarioId = Guid.NewGuid(),
            FamiliaId = Guid.NewGuid(),
            Hash = RefreshTokenOpaco.CalcularHash(RefreshValido),
            CriadoEmUtc = Agora.AddDays(-1),
            ExpiraEmUtc = expiraEmUtc ?? Agora.AddDays(6),
            ConsumidoEmUtc = consumidoEmUtc,
        };
    }

    private static ContaAutenticada NovaConta()
    {
        return new ContaAutenticada(
            Id: Guid.NewGuid(),
            Nome: "Cidadão de Teste",
            Email: "cidadao.teste@example.org",
            Papel: Papeis.Cidadao,
            OrganizacaoId: null,
            TrocaSenhaPendente: false,
            Ativa: true);
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
        public Cenario(
            RefreshToken? token = null,
            ContaAutenticada? conta = null,
            bool consumoBemSucedido = true,
            bool semToken = false)
        {
            Token = token ?? CriarToken();
            Conta = conta ?? NovaConta();
            var tokenParaConsulta = semToken ? null : Token;

            RefreshTokens
                .Setup(tokens => tokens.ObterPorHashAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(tokenParaConsulta);

            Autenticacao
                .Setup(servico => servico.ObterPorUsuarioIdAsync(
                    Token.UsuarioId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(Conta));

            RefreshTokens
                .Setup(tokens => tokens.ConsumirAsync(
                    Token.Id,
                    Token.UsuarioId,
                    Agora,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(consumoBemSucedido);

            RefreshTokens
                .Setup(tokens => tokens.AdicionarAsync(
                    It.IsAny<RefreshToken>(),
                    It.IsAny<CancellationToken>()))
                .Callback<RefreshToken, CancellationToken>((novoToken, _) =>
                    RefreshAdicionado = novoToken)
                .Returns(Task.CompletedTask);

            RefreshTokens
                .Setup(tokens => tokens.RevogarFamiliaAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Gerador
                .Setup(gerador => gerador.Gerar(It.IsAny<ContaAutenticada>(), Agora))
                .Returns(new TokenDeAcesso("token-de-acesso", Agora.AddMinutes(15)));

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

        public Mock<IGeradorDeTokenDeAcesso> Gerador { get; } = new();

        public Mock<IRepositorioDeRefreshTokens> RefreshTokens { get; } = new();

        public Mock<IUnidadeDeTrabalho> UnidadeDeTrabalho { get; } = new();

        public Mock<ITransacao> Transacao { get; } = new();

        public RefreshToken? RefreshAdicionado { get; private set; }

        public RenovarSessaoHandler CriarHandler()
        {
            return new RenovarSessaoHandler(
                Autenticacao.Object,
                Gerador.Object,
                RefreshTokens.Object,
                UnidadeDeTrabalho.Object,
                new ConfiguracaoDeSessao(TimeSpan.FromMinutes(15), TimeSpan.FromDays(7)),
                new RelogioFixo());
        }
    }
}
