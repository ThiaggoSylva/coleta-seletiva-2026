using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.PerfilDaOrganizacao;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;
using Moq;

namespace ColetaSeletiva.Application.Tests.PerfilDaOrganizacao;

[TestClass]
public sealed class DefinirPublicacaoHandlerTests
{
    private static readonly Guid OrganizacaoDeTeste = Guid.Parse("dddddddd-4444-4444-4444-444444444444");
    private static readonly Guid AdministradorId = Guid.Parse("eeeeeeee-5555-5555-5555-555555555555");
    private static readonly Guid CorrelacaoId = Guid.Parse("ffffffff-6666-6666-6666-666666666666");
    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task Publicacao_aceita_commita_e_audita()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandler().ExecutarAsync(
            new PublicacaoDaOrganizacaoEntrada(true),
            CorrelacaoId);

        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsTrue(resultado.Value.Publicada);
        Assert.AreEqual(AcoesAuditadas.PublicacaoAlterada, cenario.EventoRegistrado?.Acao);
        cenario.Transacao.Verify(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Publicacao_sem_pre_requisitos_desfaz_transacao_sem_auditar()
    {
        var cenario = new Cenario(
            Result.Fail<PublicacaoDaOrganizacaoResposta>(
                new ErroDeConflito(
                    CodigosDeErro.PublicacaoIncompleta,
                    "Habilite ao menos um tipo de resíduo antes de publicar.")));

        var resultado = await cenario.CriarHandler().ExecutarAsync(
            new PublicacaoDaOrganizacaoEntrada(true),
            CorrelacaoId);

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(
            CodigosDeErro.PublicacaoIncompleta,
            resultado.Errors.OfType<ErroDeConflito>().Single().Codigo);
        cenario.Transacao.Verify(transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.IsNull(cenario.EventoRegistrado);
    }

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Agora;
    }

    private sealed class ContextoDeTeste : ITenantContext
    {
        public bool Autenticado => true;

        public bool EscopoValido => true;

        public bool EhCidadao => false;

        public Guid? UsuarioId => AdministradorId;

        public Guid? OrganizacaoId => OrganizacaoDeTeste;
    }

    private sealed class Cenario
    {
        public Cenario(Result<PublicacaoDaOrganizacaoResposta>? resposta = null)
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
                .Setup(perfil => perfil.DefinirPublicacaoAsync(
                    OrganizacaoDeTeste,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(resposta
                    ?? Result.Ok(new PublicacaoDaOrganizacaoResposta(true, Agora)));

            Auditoria
                .Setup(auditoria => auditoria.RegistrarAsync(
                    It.IsAny<EventoDeAuditoria>(),
                    It.IsAny<CancellationToken>()))
                .Callback<EventoDeAuditoria, CancellationToken>((evento, _) => EventoRegistrado = evento)
                .Returns(Task.CompletedTask);
        }

        public Mock<IPerfilDaOrganizacao> Perfil { get; } = new();

        public Mock<IRegistroDeAuditoria> Auditoria { get; } = new();

        public Mock<IUnidadeDeTrabalho> UnidadeDeTrabalho { get; } = new();

        public Mock<ITransacao> Transacao { get; } = new();

        public EventoDeAuditoria? EventoRegistrado { get; private set; }

        public DefinirPublicacaoHandler CriarHandler()
        {
            return new DefinirPublicacaoHandler(
                new ContextoDeTeste(),
                Auditoria.Object,
                UnidadeDeTrabalho.Object,
                new RelogioFixo(),
                Perfil.Object);
        }
    }
}
