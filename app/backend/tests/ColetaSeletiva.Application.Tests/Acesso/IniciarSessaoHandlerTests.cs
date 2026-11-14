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
public sealed class IniciarSessaoHandlerTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid CorrelacaoId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [TestMethod]
    public async Task Login_valido_emite_token_e_refresh_com_hash_sem_guardar_o_valor()
    {
        var cenario = new Cenario();
        var conta = cenario.Conta;

        var resultado = await cenario.CriarHandler().ExecutarAsync(EntradaValida(), CorrelacaoId);

        Assert.IsTrue(resultado.IsSuccess, Descrever(resultado));
        Assert.AreEqual("token-de-acesso", resultado.Value.TokenDeAcesso);
        Assert.AreEqual(Agora.AddMinutes(15), resultado.Value.ExpiraEmUtc);
        Assert.AreEqual(conta, resultado.Value.Conta);

        Assert.IsNotNull(cenario.RefreshAdicionado);
        Assert.AreEqual(conta.Id, cenario.RefreshAdicionado.UsuarioId);
        Assert.AreNotEqual(Guid.Empty, cenario.RefreshAdicionado.FamiliaId);
        Assert.AreEqual(Agora, cenario.RefreshAdicionado.CriadoEmUtc);
        Assert.AreEqual(Agora.AddDays(7), cenario.RefreshAdicionado.ExpiraEmUtc);
        Assert.IsNull(cenario.RefreshAdicionado.ConsumidoEmUtc);
        Assert.IsNull(cenario.RefreshAdicionado.RevogadoEmUtc);
        Assert.AreEqual(RefreshToken.HashMaxLength, cenario.RefreshAdicionado.Hash.Length);
        Assert.AreEqual(
            RefreshTokenOpaco.CalcularHash(resultado.Value.RefreshToken),
            cenario.RefreshAdicionado.Hash);
        Assert.AreNotEqual(resultado.Value.RefreshToken, cenario.RefreshAdicionado.Hash);

        Assert.IsNotNull(cenario.EventoRegistrado);
        Assert.AreEqual(conta.OrganizacaoId, cenario.EventoRegistrado.OrganizacaoId);
        Assert.AreEqual(conta.Id, cenario.EventoRegistrado.AtorUsuarioId);
        Assert.AreEqual(AcoesAuditadas.SessaoIniciada, cenario.EventoRegistrado.Acao);
        Assert.AreEqual(EntidadesAuditadas.Usuario, cenario.EventoRegistrado.Entidade);
        Assert.AreEqual(conta.Id, cenario.EventoRegistrado.EntidadeId);
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
    public async Task Credenciais_invalidas_retornam_erro_generico_sem_criar_sessao()
    {
        var cenario = new Cenario(
            autenticacao: Result.Fail<ContaAutenticada>(
                new ErroNaoAutenticado(MensagensDeSessao.CredenciaisInvalidas)));

        var resultado = await cenario.CriarHandler().ExecutarAsync(EntradaValida(), CorrelacaoId);

        Assert.IsTrue(resultado.IsFailed);
        var erro = resultado.Errors.OfType<ErroNaoAutenticado>().Single();
        Assert.AreEqual(MensagensDeSessao.CredenciaisInvalidas, erro.Message);

        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.RefreshTokens.Verify(
            tokens => tokens.AdicionarAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.Auditoria.Verify(
            auditoria => auditoria.RegistrarAsync(
                It.IsAny<EventoDeAuditoria>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Entrada_invalida_nao_autentica_nem_inicia_transacao()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandler().ExecutarAsync(
            new LoginEntrada(string.Empty, string.Empty),
            CorrelacaoId);

        Assert.IsTrue(resultado.IsFailed);
        var campos = resultado.Errors
            .OfType<ErroDeValidacao>()
            .Select(erro => erro.Campo)
            .ToList();

        CollectionAssert.Contains(campos, nameof(LoginEntrada.Identificador));
        CollectionAssert.Contains(campos, nameof(LoginEntrada.Senha));

        cenario.Autenticacao.Verify(
            autenticacao => autenticacao.AutenticarAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static LoginEntrada EntradaValida()
    {
        return new LoginEntrada("admin.teste", "SenhaForte!123");
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
        public Cenario(Result<ContaAutenticada>? autenticacao = null)
        {
            Conta = new ContaAutenticada(
                Id: Guid.NewGuid(),
                Nome: "Administrador de Teste",
                Email: "admin.teste@example.org",
                Papel: Papeis.Administrador,
                OrganizacaoId: Guid.NewGuid(),
                TrocaSenhaPendente: false,
                Ativa: true);

            Autenticacao
                .Setup(servico => servico.AutenticarAsync(
                    "admin.teste",
                    "SenhaForte!123",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(autenticacao ?? Result.Ok(Conta));

            Gerador
                .Setup(gerador => gerador.Gerar(Conta, Agora))
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

            RefreshTokens
                .Setup(tokens => tokens.AdicionarAsync(
                    It.IsAny<RefreshToken>(),
                    It.IsAny<CancellationToken>()))
                .Callback<RefreshToken, CancellationToken>((token, _) =>
                    RefreshAdicionado = token)
                .Returns(Task.CompletedTask);

            Auditoria
                .Setup(auditoria => auditoria.RegistrarAsync(
                    It.IsAny<EventoDeAuditoria>(),
                    It.IsAny<CancellationToken>()))
                .Callback<EventoDeAuditoria, CancellationToken>((evento, _) =>
                    EventoRegistrado = evento)
                .Returns(Task.CompletedTask);
        }

        public ContaAutenticada Conta { get; }

        public Mock<IAutenticacaoDeContas> Autenticacao { get; } = new();

        public Mock<IGeradorDeTokenDeAcesso> Gerador { get; } = new();

        public Mock<IRepositorioDeRefreshTokens> RefreshTokens { get; } = new();

        public Mock<IRegistroDeAuditoria> Auditoria { get; } = new();

        public Mock<IUnidadeDeTrabalho> UnidadeDeTrabalho { get; } = new();

        public Mock<ITransacao> Transacao { get; } = new();

        public RefreshToken? RefreshAdicionado { get; private set; }

        public EventoDeAuditoria? EventoRegistrado { get; private set; }

        public IniciarSessaoHandler CriarHandler()
        {
            return new IniciarSessaoHandler(
                new LoginValidator(),
                Autenticacao.Object,
                Gerador.Object,
                RefreshTokens.Object,
                Auditoria.Object,
                UnidadeDeTrabalho.Object,
                new ConfiguracaoDeSessao(TimeSpan.FromMinutes(15), TimeSpan.FromDays(7)),
                new RelogioFixo());
        }
    }
}
