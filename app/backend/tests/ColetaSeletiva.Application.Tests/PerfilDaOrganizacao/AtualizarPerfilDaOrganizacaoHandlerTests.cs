using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Catalogo;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.PerfilDaOrganizacao;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Seguranca;
using ColetaSeletiva.Domain.Entidades;
using FluentResults;
using Moq;

namespace ColetaSeletiva.Application.Tests.PerfilDaOrganizacao;

[TestClass]
public sealed class AtualizarPerfilDaOrganizacaoHandlerTests
{
    private static readonly Guid OrganizacaoId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
    private static readonly Guid AdministradorId = Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222");
    private static readonly Guid CorrelacaoId = Guid.Parse("cccccccc-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task Atualizacao_valida_commita_e_audita()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandler().ExecutarAsync(EntradaValida(), CorrelacaoId);

        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual("Cooperativa Atualizada", cenario.EntradaRecebida?.Nome);
        Assert.IsNotNull(cenario.EventoRegistrado);
        Assert.AreEqual(AcoesAuditadas.OrganizacaoAtualizada, cenario.EventoRegistrado.Acao);
        Assert.AreEqual(OrganizacaoId, cenario.EventoRegistrado.OrganizacaoId);
        Assert.AreEqual(AdministradorId, cenario.EventoRegistrado.AtorUsuarioId);
        Assert.AreEqual(CorrelacaoId, cenario.EventoRegistrado.CorrelacaoId);
        cenario.Transacao.Verify(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Fuso_invalido_nao_inicia_transacao_nem_persiste()
    {
        var cenario = new Cenario();
        var entrada = EntradaValida() with { Fuso = "America/Atlantida" };

        var resultado = await cenario.CriarHandler().ExecutarAsync(entrada, CorrelacaoId);

        Assert.IsTrue(resultado.IsFailed);
        CollectionAssert.Contains(
            resultado.Errors.OfType<ErroDeValidacao>().Select(erro => erro.Campo).ToList(),
            nameof(AtualizarPerfilDaOrganizacaoEntrada.Fuso));
        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.Perfil.Verify(
            perfil => perfil.AtualizarAsync(
                It.IsAny<Guid>(),
                It.IsAny<AtualizarPerfilDaOrganizacaoEntrada>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Cidadao_nao_atualiza_perfil_da_organizacao()
    {
        var cenario = new Cenario(ehCidadao: true);

        var resultado = await cenario.CriarHandler().ExecutarAsync(EntradaValida(), CorrelacaoId);

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroDeAcessoNegado>(resultado.Errors.Single());
        cenario.Perfil.Verify(
            perfil => perfil.AtualizarAsync(
                It.IsAny<Guid>(),
                It.IsAny<AtualizarPerfilDaOrganizacaoEntrada>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Falha_de_persistencia_desfaz_transacao_e_nao_audita()
    {
        var cenario = new Cenario(
            resposta: Result.Fail<PerfilDaOrganizacaoResposta>(
                new ErroDeConflito(CodigosDeErro.VinculoEmUso, "Vínculo em uso.")));

        var resultado = await cenario.CriarHandler().ExecutarAsync(EntradaValida(), CorrelacaoId);

        Assert.IsTrue(resultado.IsFailed);
        cenario.Transacao.Verify(transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        cenario.Transacao.Verify(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.IsNull(cenario.EventoRegistrado);
    }

    private static AtualizarPerfilDaOrganizacaoEntrada EntradaValida()
    {
        return new AtualizarPerfilDaOrganizacaoEntrada(
            Nome: "Cooperativa Atualizada",
            Tipo: TipoOrganizacao.Cooperativa,
            ContatoEmail: "contato@example.org",
            ContatoTelefone: "(11) 3333-4444",
            Fuso: "America/Sao_Paulo",
            LimiteSolicitacoesAbertas: 3);
    }

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Agora;
    }

    private sealed class ContextoDeTeste : ITenantContext
    {
        public bool Autenticado { get; init; }

        public bool EscopoValido { get; init; }

        public bool EhCidadao { get; init; }

        public Guid? UsuarioId { get; init; }

        public Guid? OrganizacaoId { get; init; }
    }

    private sealed class Cenario
    {
        public Cenario(bool ehCidadao = false, Result<PerfilDaOrganizacaoResposta>? resposta = null)
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

            Perfil
                .Setup(perfil => perfil.AtualizarAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<AtualizarPerfilDaOrganizacaoEntrada>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Guid, AtualizarPerfilDaOrganizacaoEntrada, CancellationToken>(
                    (_, entrada, _) => EntradaRecebida = entrada)
                .ReturnsAsync(resposta ?? Result.Ok(PerfilDeTeste()));

            Auditoria
                .Setup(auditoria => auditoria.RegistrarAsync(
                    It.IsAny<EventoDeAuditoria>(),
                    It.IsAny<CancellationToken>()))
                .Callback<EventoDeAuditoria, CancellationToken>((evento, _) => EventoRegistrado = evento)
                .Returns(Task.CompletedTask);

            Contexto = new ContextoDeTeste
            {
                Autenticado = true,
                EscopoValido = true,
                EhCidadao = ehCidadao,
                UsuarioId = AdministradorId,
                OrganizacaoId = ehCidadao ? null : OrganizacaoId,
            };
        }

        public Mock<IPerfilDaOrganizacao> Perfil { get; } = new();

        public Mock<IRegistroDeAuditoria> Auditoria { get; } = new();

        public Mock<IUnidadeDeTrabalho> UnidadeDeTrabalho { get; } = new();

        public Mock<ITransacao> Transacao { get; } = new();

        public AtualizarPerfilDaOrganizacaoEntrada? EntradaRecebida { get; private set; }

        public EventoDeAuditoria? EventoRegistrado { get; private set; }

        private ITenantContext Contexto { get; }

        public AtualizarPerfilDaOrganizacaoHandler CriarHandler()
        {
            return new AtualizarPerfilDaOrganizacaoHandler(
                Contexto,
                Auditoria.Object,
                UnidadeDeTrabalho.Object,
                new RelogioFixo(),
                new AtualizarPerfilDaOrganizacaoValidator(),
                Perfil.Object);
        }

        private static PerfilDaOrganizacaoResposta PerfilDeTeste()
        {
            return new PerfilDaOrganizacaoResposta(
                Id: OrganizacaoId,
                Nome: "Cooperativa Atualizada",
                Tipo: TipoOrganizacao.Cooperativa.ToString(),
                ContatoEmail: "contato@example.org",
                ContatoTelefone: "(11) 3333-4444",
                Fuso: "America/Sao_Paulo",
                LimiteSolicitacoesAbertas: 3,
                Publicada: false,
                Municipios: [],
                Periodos: [],
                Tipos: []);
        }
    }
}
