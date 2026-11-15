using ColetaSeletiva.Application.Acesso;
using ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;
using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Identidade;
using FluentResults;
using Moq;

namespace ColetaSeletiva.Application.Tests.Acesso;

[TestClass]
public sealed class RegistrarOrganizacaoHandlerTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 11, 15, 0, 0, TimeSpan.Zero);
    private static readonly Guid CorrelacaoId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [TestMethod]
    public async Task Registro_valido_cria_organizacao_administrador_e_auditoria_na_mesma_transacao()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandler().ExecutarAsync(EntradaValida(), CorrelacaoId);

        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreNotEqual(Guid.Empty, resultado.Value.OrganizacaoId);
        Assert.AreNotEqual(Guid.Empty, resultado.Value.AdministradorId);

        Assert.IsNotNull(cenario.OrganizacaoAdicionada);
        Assert.AreEqual(resultado.Value.OrganizacaoId, cenario.OrganizacaoAdicionada.Id);
        Assert.IsFalse(cenario.OrganizacaoAdicionada.Publicada);
        Assert.AreEqual(TipoOrganizacao.Cooperativa, cenario.OrganizacaoAdicionada.Tipo);

        Assert.IsNotNull(cenario.ContaCriada);
        Assert.AreEqual(resultado.Value.OrganizacaoId, cenario.ContaCriada.OrganizacaoId);
        Assert.AreEqual("admin.teste", cenario.ContaCriada.Usuario);
        Assert.AreEqual(Papeis.Administrador, cenario.PapelAtribuido);
        Assert.IsFalse(cenario.ContaCriada.TrocaSenhaPendente);

        Assert.IsNotNull(cenario.EventoRegistrado);
        Assert.AreEqual(resultado.Value.OrganizacaoId, cenario.EventoRegistrado.OrganizacaoId);
        Assert.AreEqual(resultado.Value.AdministradorId, cenario.EventoRegistrado.AtorUsuarioId);
        Assert.AreEqual(AcoesAuditadas.OrganizacaoRegistrada, cenario.EventoRegistrado.Acao);
        Assert.AreEqual(nameof(Organizacao), cenario.EventoRegistrado.Entidade);
        Assert.AreEqual(resultado.Value.OrganizacaoId, cenario.EventoRegistrado.EntidadeId);
        Assert.AreEqual(CorrelacaoId, cenario.EventoRegistrado.CorrelacaoId);
        Assert.AreEqual(Agora, cenario.EventoRegistrado.OcorridoEmUtc);
        Assert.IsNull(cenario.EventoRegistrado.CamposAlterados);

        cenario.Transacao.Verify(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        cenario.Transacao.Verify(transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Conta_duplicada_desfaz_transacao_e_nao_registra_auditoria()
    {
        var cenario = new Cenario(
            criacaoDaConta: Result.Fail<Guid>(new ErroDeConflito(
                CodigosDeErro.EmailEmUso,
                "O e-mail informado já está em uso.")));

        var resultado = await cenario.CriarHandler().ExecutarAsync(EntradaValida(), CorrelacaoId);

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroDeConflito>(resultado.Errors.Single());
        Assert.AreEqual(CodigosDeErro.EmailEmUso, ((ErroDeConflito)resultado.Errors.Single()).Codigo);

        cenario.Transacao.Verify(transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        cenario.Transacao.Verify(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.IsNull(cenario.EventoRegistrado);
        cenario.Auditoria.Verify(
            auditoria => auditoria.RegistrarAsync(It.IsAny<EventoDeAuditoria>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Entrada_invalida_nao_inicia_transacao_nem_persiste_registros()
    {
        var cenario = new Cenario();
        var entrada = new RegistroDeOrganizacaoEntrada(
            NomeOrganizacao: string.Empty,
            Tipo: 0,
            Usuario: string.Empty,
            Email: "email-invalido",
            Senha: string.Empty);

        var resultado = await cenario.CriarHandler().ExecutarAsync(entrada, CorrelacaoId);

        Assert.IsTrue(resultado.IsFailed);
        var campos = resultado.Errors
            .OfType<ErroDeValidacao>()
            .Select(erro => erro.Campo)
            .ToList();

        CollectionAssert.Contains(campos, nameof(RegistroDeOrganizacaoEntrada.NomeOrganizacao));
        CollectionAssert.Contains(campos, nameof(RegistroDeOrganizacaoEntrada.Tipo));
        CollectionAssert.Contains(campos, nameof(RegistroDeOrganizacaoEntrada.Usuario));
        CollectionAssert.Contains(campos, nameof(RegistroDeOrganizacaoEntrada.Email));
        CollectionAssert.Contains(campos, nameof(RegistroDeOrganizacaoEntrada.Senha));

        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.Organizacoes.Verify(
            organizacoes => organizacoes.AdicionarAsync(It.IsAny<Organizacao>(), It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.Contas.Verify(
            contas => contas.CriarAsync(It.IsAny<ContaNova>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Falha_na_auditoria_desfaz_a_transacao()
    {
        var cenario = new Cenario(
            erroDeAuditoria: new InvalidOperationException("Falha simulada na auditoria."));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => cenario.CriarHandler().ExecutarAsync(EntradaValida(), CorrelacaoId));

        cenario.Transacao.Verify(transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        cenario.Transacao.Verify(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Correlacao_vazia_e_rejeitada_antes_de_persistir()
    {
        var cenario = new Cenario();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => cenario.CriarHandler().ExecutarAsync(EntradaValida(), Guid.Empty));

        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static RegistroDeOrganizacaoEntrada EntradaValida()
    {
        return new RegistroDeOrganizacaoEntrada(
            NomeOrganizacao: "Cooperativa de Teste",
            Tipo: TipoOrganizacao.Cooperativa,
            Usuario: "admin.teste",
            Email: "admin.teste@example.org",
            Senha: "SenhaForte!123");
    }

    private sealed class RelogioFixo : TimeProvider
    {
        private readonly DateTimeOffset _agora;

        public RelogioFixo(DateTimeOffset agora)
        {
            _agora = agora;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _agora;
        }
    }

    private sealed class Cenario
    {
        public Cenario(
            Result<Guid>? criacaoDaConta = null,
            Exception? erroDeAuditoria = null)
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

            Organizacoes
                .Setup(organizacoes => organizacoes.AdicionarAsync(
                    It.IsAny<Organizacao>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Organizacao, CancellationToken>((organizacao, _) =>
                    OrganizacaoAdicionada = organizacao)
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

            var registroDeAuditoria = Auditoria.Setup(auditoria => auditoria.RegistrarAsync(
                It.IsAny<EventoDeAuditoria>(),
                It.IsAny<CancellationToken>()));

            if (erroDeAuditoria is null)
            {
                registroDeAuditoria
                    .Callback<EventoDeAuditoria, CancellationToken>((evento, _) =>
                        EventoRegistrado = evento)
                    .Returns(Task.CompletedTask);
            }
            else
            {
                registroDeAuditoria.ThrowsAsync(erroDeAuditoria);
            }
        }

        public Mock<IRegistroDeOrganizacoes> Organizacoes { get; } = new();

        public Mock<ICadastroDeContas> Contas { get; } = new();

        public Mock<IRegistroDeAuditoria> Auditoria { get; } = new();

        public Mock<IUnidadeDeTrabalho> UnidadeDeTrabalho { get; } = new();

        public Mock<ITransacao> Transacao { get; } = new();

        public Organizacao? OrganizacaoAdicionada { get; private set; }

        public ContaNova? ContaCriada { get; private set; }

        public string? PapelAtribuido { get; private set; }

        public EventoDeAuditoria? EventoRegistrado { get; private set; }

        public RegistrarOrganizacaoHandler CriarHandler()
        {
            return new RegistrarOrganizacaoHandler(
                new RegistroDeOrganizacaoValidator(),
                Organizacoes.Object,
                Contas.Object,
                Auditoria.Object,
                UnidadeDeTrabalho.Object,
                new RelogioFixo(Agora));
        }
    }
}
