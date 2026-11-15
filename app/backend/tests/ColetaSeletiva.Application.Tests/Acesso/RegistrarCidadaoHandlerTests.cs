using ColetaSeletiva.Application.Acesso;
using ColetaSeletiva.Application.Acesso.RegistroDeCidadao;
using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Domain.Identidade;
using FluentResults;
using Moq;

namespace ColetaSeletiva.Application.Tests.Acesso;

[TestClass]
public sealed class RegistrarCidadaoHandlerTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 11, 16, 0, 0, TimeSpan.Zero);
    private static readonly Guid CorrelacaoId = Guid.Parse("cccccccc-dddd-eeee-ffff-aaaaaaaaaaaa");

    [TestMethod]
    public async Task Registro_valido_cria_conta_sem_organizacao_com_papel_cidadao_e_auditoria()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandler().ExecutarAsync(EntradaValida(), CorrelacaoId);

        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreNotEqual(Guid.Empty, resultado.Value.UsuarioId);

        Assert.IsNotNull(cenario.ContaCriada);
        Assert.IsNull(cenario.ContaCriada.OrganizacaoId);
        Assert.AreEqual("cidadao.teste@example.org", cenario.ContaCriada.Usuario);
        Assert.AreEqual(Papeis.Cidadao, cenario.PapelAtribuido);
        Assert.IsFalse(cenario.ContaCriada.TrocaSenhaPendente);

        Assert.IsNotNull(cenario.EventoRegistrado);
        Assert.IsNull(cenario.EventoRegistrado.OrganizacaoId);
        Assert.AreEqual(resultado.Value.UsuarioId, cenario.EventoRegistrado.AtorUsuarioId);
        Assert.AreEqual(AcoesAuditadas.CidadaoRegistrado, cenario.EventoRegistrado.Acao);
        Assert.AreEqual(EntidadesAuditadas.Usuario, cenario.EventoRegistrado.Entidade);
        Assert.AreEqual(resultado.Value.UsuarioId, cenario.EventoRegistrado.EntidadeId);
        Assert.AreEqual(CorrelacaoId, cenario.EventoRegistrado.CorrelacaoId);
        Assert.IsNull(cenario.EventoRegistrado.CamposAlterados);

        cenario.Transacao.Verify(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        cenario.Transacao.Verify(transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Email_duplicado_desfaz_transacao_e_nao_registra_auditoria()
    {
        var cenario = new Cenario(
            criacaoDaConta: Result.Fail<Guid>(new ErroDeConflito(
                CodigosDeErro.EmailEmUso,
                "O e-mail informado já está em uso.")));

        var resultado = await cenario.CriarHandler().ExecutarAsync(EntradaValida(), CorrelacaoId);

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroDeConflito>(resultado.Errors.Single());

        cenario.Transacao.Verify(transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        cenario.Transacao.Verify(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        cenario.Auditoria.Verify(
            auditoria => auditoria.RegistrarAsync(It.IsAny<EventoDeAuditoria>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Entrada_invalida_nao_inicia_transacao_nem_cria_conta()
    {
        var cenario = new Cenario();
        var entrada = new RegistroDeCidadaoEntrada(
            Nome: string.Empty,
            Email: "email-invalido",
            Telefone: string.Empty,
            Senha: string.Empty);

        var resultado = await cenario.CriarHandler().ExecutarAsync(entrada, CorrelacaoId);

        Assert.IsTrue(resultado.IsFailed);
        var campos = resultado.Errors
            .OfType<ErroDeValidacao>()
            .Select(erro => erro.Campo)
            .ToList();

        CollectionAssert.Contains(campos, nameof(RegistroDeCidadaoEntrada.Nome));
        CollectionAssert.Contains(campos, nameof(RegistroDeCidadaoEntrada.Email));
        CollectionAssert.Contains(campos, nameof(RegistroDeCidadaoEntrada.Telefone));
        CollectionAssert.Contains(campos, nameof(RegistroDeCidadaoEntrada.Senha));

        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.Contas.Verify(
            contas => contas.CriarAsync(It.IsAny<ContaNova>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static RegistroDeCidadaoEntrada EntradaValida()
    {
        return new RegistroDeCidadaoEntrada(
            Nome: "Cidadão de Teste",
            Email: "cidadao.teste@example.org",
            Telefone: "(11) 99999-0000",
            Senha: "SenhaForte!123");
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
        public Cenario(Result<Guid>? criacaoDaConta = null)
        {
            UnidadeDeTrabalho
                .Setup(unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Transacao.Object);

            Transacao
                .Setup(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Transacao
                .Setup(transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Contas
                .Setup(contas => contas.CriarAsync(
                    It.IsAny<ContaNova>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ContaNova, string, CancellationToken>((conta, papel, _) =>
                {
                    ContaCriada = conta;
                    PapelAtribuido = papel;
                })
                .ReturnsAsync(criacaoDaConta ?? Result.Ok(Guid.NewGuid()));

            Auditoria
                .Setup(auditoria => auditoria.RegistrarAsync(
                    It.IsAny<EventoDeAuditoria>(),
                    It.IsAny<CancellationToken>()))
                .Callback<EventoDeAuditoria, CancellationToken>((evento, _) =>
                    EventoRegistrado = evento)
                .Returns(Task.CompletedTask);
        }

        public Mock<ICadastroDeContas> Contas { get; } = new();

        public Mock<IRegistroDeAuditoria> Auditoria { get; } = new();

        public Mock<IUnidadeDeTrabalho> UnidadeDeTrabalho { get; } = new();

        public Mock<ITransacao> Transacao { get; } = new();

        public ContaNova? ContaCriada { get; private set; }

        public string? PapelAtribuido { get; private set; }

        public EventoDeAuditoria? EventoRegistrado { get; private set; }

        public RegistrarCidadaoHandler CriarHandler()
        {
            return new RegistrarCidadaoHandler(
                new RegistroDeCidadaoValidator(),
                Contas.Object,
                Auditoria.Object,
                UnidadeDeTrabalho.Object,
                new RelogioFixo());
        }
    }
}
