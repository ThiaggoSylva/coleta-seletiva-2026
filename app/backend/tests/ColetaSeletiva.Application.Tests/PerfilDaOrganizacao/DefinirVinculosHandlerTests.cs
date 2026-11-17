using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Catalogo;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.PerfilDaOrganizacao;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;
using Moq;

namespace ColetaSeletiva.Application.Tests.PerfilDaOrganizacao;

[TestClass]
public sealed class DefinirVinculosHandlerTests
{
    private static readonly Guid OrganizacaoDeTeste = Guid.Parse("dddddddd-4444-4444-4444-444444444444");
    private static readonly Guid AdministradorId = Guid.Parse("eeeeeeee-5555-5555-5555-555555555555");
    private static readonly Guid TipoPapel = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TipoMetal = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task Municipios_validos_commitam_e_auditam()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandlerDeMunicipios().ExecutarAsync(
            new DefinirMunicipiosEntrada([5208707, 5201405]),
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsSuccess);
        cenario.Perfil.Verify(perfil => perfil.DefinirMunicipiosAsync(
            OrganizacaoDeTeste,
            It.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 5208707, 5201405 })),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.AreEqual(AcoesAuditadas.MunicipiosAtualizados, cenario.EventoRegistrado?.Acao);
        cenario.Transacao.Verify(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        cenario.Transacao.Verify(transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Municipio_em_uso_desfaz_transacao_sem_auditar()
    {
        var cenario = new Cenario(
            Result.Fail<PerfilDaOrganizacaoResposta>(
                new ErroDeConflito(
                    CodigosDeErro.VinculoEmUso,
                    "Remova ou altere os pontos que usam este município antes.")));

        var resultado = await cenario.CriarHandlerDeMunicipios().ExecutarAsync(
            new DefinirMunicipiosEntrada([5208707]),
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(
            CodigosDeErro.VinculoEmUso,
            resultado.Errors.OfType<ErroDeConflito>().Single().Codigo);
        cenario.Transacao.Verify(transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        cenario.Transacao.Verify(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.IsNull(cenario.EventoRegistrado);
    }

    [TestMethod]
    public async Task Municipios_repetidos_falham_na_validacao_sem_iniciar_transacao()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandlerDeMunicipios().ExecutarAsync(
            new DefinirMunicipiosEntrada([5208707, 5208707]),
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(
            nameof(DefinirMunicipiosEntrada.MunicipioIds),
            resultado.Errors.OfType<ErroDeValidacao>().Single().Campo);
        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.Perfil.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Periodos_validos_commitam_e_auditam()
    {
        var cenario = new Cenario();
        var periodos = new[]
        {
            new PeriodoDaOrganizacaoEntrada("Manha", "08:00", "12:00"),
            new PeriodoDaOrganizacaoEntrada("Tarde", "13:00", "17:00"),
        };

        var resultado = await cenario.CriarHandlerDePeriodos().ExecutarAsync(
            new DefinirPeriodosEntrada(periodos),
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual(AcoesAuditadas.PeriodosAtualizados, cenario.EventoRegistrado?.Acao);
        cenario.Transacao.Verify(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Periodo_com_fim_anterior_ao_inicio_falha_na_validacao()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandlerDePeriodos().ExecutarAsync(
            new DefinirPeriodosEntrada(
                [new PeriodoDaOrganizacaoEntrada("Manha", "12:00", "08:00")]),
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsTrue(resultado.Errors.OfType<ErroDeValidacao>().Any());
        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Tipos_validos_commitam_e_auditam()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandlerDeTipos().ExecutarAsync(
            new DefinirTiposHabilitadosEntrada(
                [new TipoHabilitadoEntrada(TipoPapel, AceitaRetiradaDomiciliar: true)]),
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual(AcoesAuditadas.TiposHabilitadosAtualizados, cenario.EventoRegistrado?.Acao);
        cenario.Transacao.Verify(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Tipo_em_uso_desfaz_transacao_sem_auditar()
    {
        var cenario = new Cenario(
            Result.Fail<PerfilDaOrganizacaoResposta>(
                new ErroDeConflito(
                    CodigosDeErro.VinculoEmUso,
                    "Desabilite o tipo apenas depois de ajustar os pontos que o utilizam.")));

        var resultado = await cenario.CriarHandlerDeTipos().ExecutarAsync(
            new DefinirTiposHabilitadosEntrada([new TipoHabilitadoEntrada(TipoMetal, false)]),
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(
            CodigosDeErro.VinculoEmUso,
            resultado.Errors.OfType<ErroDeConflito>().Single().Codigo);
        cenario.Transacao.Verify(transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.IsNull(cenario.EventoRegistrado);
    }

    [TestMethod]
    public async Task Cidadao_recebe_acesso_negado_sem_consultar_o_perfil()
    {
        var cenario = new Cenario(contexto: new ContextoDeTeste(ehCidadao: true));

        var resultado = await cenario.CriarHandlerDeMunicipios().ExecutarAsync(
            new DefinirMunicipiosEntrada([5208707]),
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroDeAcessoNegado>(resultado.Errors.Single());
        cenario.Perfil.VerifyNoOtherCalls();
        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Sessao_nao_autenticada_recebe_erro_de_autenticacao()
    {
        var cenario = new Cenario(contexto: new ContextoDeTeste(autenticado: false));

        var resultado = await cenario.CriarHandlerDeTipos().ExecutarAsync(
            new DefinirTiposHabilitadosEntrada([new TipoHabilitadoEntrada(TipoPapel, false)]),
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroNaoAutenticado>(resultado.Errors.Single());
        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Agora;
    }

    private sealed class ContextoDeTeste : ITenantContext
    {
        public ContextoDeTeste(bool autenticado = true, bool ehCidadao = false)
        {
            Autenticado = autenticado;
            EhCidadao = ehCidadao;
        }

        public bool Autenticado { get; }

        public bool EscopoValido => Autenticado;

        public bool EhCidadao { get; }

        public Guid? UsuarioId => Autenticado ? AdministradorId : null;

        public Guid? OrganizacaoId => Autenticado && !EhCidadao ? OrganizacaoDeTeste : null;
    }

    private sealed class Cenario
    {
        public Cenario(
            Result<PerfilDaOrganizacaoResposta>? resposta = null,
            ITenantContext? contexto = null)
        {
            Contexto = contexto ?? new ContextoDeTeste();

            UnidadeDeTrabalho
                .Setup(unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Transacao.Object);
            Transacao
                .Setup(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Transacao
                .Setup(transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var perfil = resposta ?? Result.Ok(PerfilVazio());
            Perfil
                .Setup(item => item.DefinirMunicipiosAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IReadOnlyCollection<int>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(perfil);
            Perfil
                .Setup(item => item.DefinirPeriodosAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IReadOnlyCollection<PeriodoDaOrganizacaoEntrada>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(perfil);
            Perfil
                .Setup(item => item.DefinirTiposAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IReadOnlyCollection<TipoHabilitadoEntrada>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(perfil);

            Auditoria
                .Setup(item => item.RegistrarAsync(
                    It.IsAny<EventoDeAuditoria>(),
                    It.IsAny<CancellationToken>()))
                .Callback<EventoDeAuditoria, CancellationToken>((evento, _) => EventoRegistrado = evento)
                .Returns(Task.CompletedTask);
        }

        public ITenantContext Contexto { get; }

        public Mock<IPerfilDaOrganizacao> Perfil { get; } = new();

        public Mock<IRegistroDeAuditoria> Auditoria { get; } = new();

        public Mock<IUnidadeDeTrabalho> UnidadeDeTrabalho { get; } = new();

        public Mock<ITransacao> Transacao { get; } = new();

        public EventoDeAuditoria? EventoRegistrado { get; private set; }

        public DefinirMunicipiosHandler CriarHandlerDeMunicipios()
        {
            return new DefinirMunicipiosHandler(
                Contexto,
                Auditoria.Object,
                UnidadeDeTrabalho.Object,
                new RelogioFixo(),
                new DefinirMunicipiosValidator(),
                Perfil.Object);
        }

        public DefinirPeriodosHandler CriarHandlerDePeriodos()
        {
            return new DefinirPeriodosHandler(
                Contexto,
                Auditoria.Object,
                UnidadeDeTrabalho.Object,
                new RelogioFixo(),
                new DefinirPeriodosValidator(),
                Perfil.Object);
        }

        public DefinirTiposHabilitadosHandler CriarHandlerDeTipos()
        {
            return new DefinirTiposHabilitadosHandler(
                Contexto,
                Auditoria.Object,
                UnidadeDeTrabalho.Object,
                new RelogioFixo(),
                new DefinirTiposHabilitadosValidator(),
                Perfil.Object);
        }
    }

    private static PerfilDaOrganizacaoResposta PerfilVazio()
    {
        return new PerfilDaOrganizacaoResposta(
            OrganizacaoDeTeste,
            "Cooperativa Verde",
            "Cooperativa",
            null,
            null,
            "America/Sao_Paulo",
            10,
            false,
            [],
            [],
            []);
    }
}
