using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Catalogo;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Application.Pontos;
using ColetaSeletiva.Application.Seguranca;
using ColetaSeletiva.Domain.Entidades;
using FluentResults;
using Moq;

namespace ColetaSeletiva.Application.Tests.Pontos;

[TestClass]
public sealed class PontosHandlersTests
{
    private static readonly Guid OrganizacaoDeTeste = Guid.Parse("dddddddd-4444-4444-4444-444444444444");
    private static readonly Guid AdministradorId = Guid.Parse("eeeeeeee-5555-5555-5555-555555555555");
    private static readonly Guid PontoId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
    private static readonly Guid TipoPapel = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task Criacao_valida_commitam_e_auditam_o_ponto()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandlerDeCriacao().ExecutarAsync(
            EntradaValida(),
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual(PontoId, resultado.Value.Id);
        cenario.Pontos.Verify(pontos => pontos.CriarAsync(
            OrganizacaoDeTeste,
            It.IsAny<CriarPontoEntrada>(),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.AreEqual(AcoesAuditadas.PontoCriado, cenario.EventoRegistrado?.Acao);
        Assert.AreEqual(EntidadesAuditadas.PontoColeta, cenario.EventoRegistrado?.Entidade);
        cenario.Transacao.Verify(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Nome_em_uso_desfaz_transacao_sem_auditar()
    {
        var cenario = new Cenario(
            Result.Fail<PontoDetalhe>(
                new ErroDeConflito(
                    CodigosDeErro.NomeEmUso,
                    "Já existe um ponto com este nome na organização.")));

        var resultado = await cenario.CriarHandlerDeCriacao().ExecutarAsync(
            EntradaValida(),
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(
            CodigosDeErro.NomeEmUso,
            resultado.Errors.OfType<ErroDeConflito>().Single().Codigo);
        cenario.Transacao.Verify(transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        cenario.Transacao.Verify(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.IsNull(cenario.EventoRegistrado);
    }

    [TestMethod]
    public async Task Entrada_sem_horarios_falha_na_validacao_sem_iniciar_transacao()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandlerDeCriacao().ExecutarAsync(
            EntradaValida() with { Horarios = [] },
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(
            nameof(CriarPontoEntrada.Horarios),
            resultado.Errors.OfType<ErroDeValidacao>().Single().Campo);
        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        cenario.Pontos.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Cidadao_nao_cria_ponto()
    {
        var cenario = new Cenario(contexto: new ContextoDeTeste(ehCidadao: true));

        var resultado = await cenario.CriarHandlerDeCriacao().ExecutarAsync(
            EntradaValida(),
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroDeAcessoNegado>(resultado.Errors.Single());
        cenario.Pontos.VerifyNoOtherCalls();
        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Ponto_de_outra_organizacao_retorna_nao_encontrado_e_desfaz_transacao()
    {
        var cenario = new Cenario(
            Result.Fail<PontoDetalhe>(new ErroNaoEncontrado("Ponto de coleta não encontrado.")));

        var resultado = await cenario.CriarHandlerDeAtualizacao().ExecutarAsync(
            Guid.NewGuid(),
            EntradaDeAtualizacaoValida(),
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroNaoEncontrado>(resultado.Errors.Single());
        cenario.Transacao.Verify(transacao => transacao.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.IsNull(cenario.EventoRegistrado);
    }

    [TestMethod]
    public async Task Status_invalido_falha_na_validacao_sem_iniciar_transacao()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandlerDeStatus().ExecutarAsync(
            PontoId,
            new AlterarStatusPontoEntrada("Fechado"),
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(
            nameof(AlterarStatusPontoEntrada.Status),
            resultado.Errors.OfType<ErroDeValidacao>().Single().Campo);
        cenario.UnidadeDeTrabalho.Verify(
            unidade => unidade.IniciarAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Alteracao_de_status_valida_audita_e_commita()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandlerDeStatus().ExecutarAsync(
            PontoId,
            new AlterarStatusPontoEntrada("indisponivelTemporariamente"),
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsSuccess);
        cenario.Pontos.Verify(pontos => pontos.AlterarStatusAsync(
            OrganizacaoDeTeste,
            PontoId,
            StatusPonto.IndisponivelTemporariamente,
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.AreEqual(AcoesAuditadas.PontoStatusAlterado, cenario.EventoRegistrado?.Acao);
        cenario.Transacao.Verify(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Retirada_logica_audita_e_commita()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandlerDeRetirada().ExecutarAsync(
            PontoId,
            Guid.NewGuid());

        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual(AcoesAuditadas.PontoRetirado, cenario.EventoRegistrado?.Acao);
        cenario.Transacao.Verify(transacao => transacao.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Operador_consulta_opcoes_da_propria_organizacao()
    {
        var cenario = new Cenario();
        cenario.Pontos
            .Setup(pontos => pontos.ListarOpcoesAsync(
                OrganizacaoDeTeste,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new OpcaoDePonto(PontoId, "Ponto Centro", "Goiânia", "GO", [])]);

        var resultado = await cenario.CriarHandlerDeOpcoes().ExecutarAsync();

        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual(1, resultado.Value.Count);
    }

    [TestMethod]
    public async Task Cidadao_nao_consulta_opcoes()
    {
        var cenario = new Cenario(contexto: new ContextoDeTeste(ehCidadao: true));

        var resultado = await cenario.CriarHandlerDeOpcoes().ExecutarAsync();

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroDeAcessoNegado>(resultado.Errors.Single());
        cenario.Pontos.VerifyNoOtherCalls();
    }

    private static CriarPontoEntrada EntradaValida()
    {
        return new CriarPontoEntrada(
            Nome: "Ponto Centro",
            MunicipioId: 5208707,
            Bairro: "Setor Central",
            Rua: "Rua do Ouro",
            Numero: "100",
            Complemento: null,
            Cep: "74015010",
            Latitude: -16.6869,
            Longitude: -49.2648,
            Observacoes: null,
            Status: "Ativo",
            Horarios: [new HorarioDoPontoEntrada("Monday", "08:00", "17:00")],
            TiposResiduoIds: [TipoPapel]);
    }

    private static AtualizarPontoEntrada EntradaDeAtualizacaoValida()
    {
        var entrada = EntradaValida();

        return new AtualizarPontoEntrada(
            entrada.Nome,
            entrada.MunicipioId,
            entrada.Bairro,
            entrada.Rua,
            entrada.Numero,
            entrada.Complemento,
            entrada.Cep,
            entrada.Latitude,
            entrada.Longitude,
            entrada.Observacoes,
            entrada.Status,
            entrada.Horarios,
            entrada.TiposResiduoIds);
    }

    private static PontoDetalhe Detalhe()
    {
        return new PontoDetalhe(
            PontoId,
            "Ponto Centro",
            new MunicipioDoCatalogo(5208707, "Goiânia", "GO"),
            "GO",
            "Setor Central",
            "Rua do Ouro",
            "100",
            null,
            "74015010",
            -16.6869,
            -49.2648,
            null,
            "Ativo",
            false,
            false,
            [new HorarioDoPontoResposta("Monday", "08:00", "17:00")],
            [new TipoDoPontoResposta(TipoPapel, "papel", "Papel", "kg")]);
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
            Result<PontoDetalhe>? resposta = null,
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

            var resultado = resposta ?? Result.Ok(Detalhe());
            Pontos
                .Setup(pontos => pontos.CriarAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CriarPontoEntrada>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(resultado);
            Pontos
                .Setup(pontos => pontos.AtualizarAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<AtualizarPontoEntrada>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(resultado);
            Pontos
                .Setup(pontos => pontos.AlterarStatusAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<StatusPonto>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(resultado);
            Pontos
                .Setup(pontos => pontos.RetirarAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(resultado);

            Auditoria
                .Setup(item => item.RegistrarAsync(
                    It.IsAny<EventoDeAuditoria>(),
                    It.IsAny<CancellationToken>()))
                .Callback<EventoDeAuditoria, CancellationToken>((evento, _) => EventoRegistrado = evento)
                .Returns(Task.CompletedTask);
        }

        public ITenantContext Contexto { get; }

        public Mock<IPontosDeColeta> Pontos { get; } = new();

        public Mock<IRegistroDeAuditoria> Auditoria { get; } = new();

        public Mock<IUnidadeDeTrabalho> UnidadeDeTrabalho { get; } = new();

        public Mock<ITransacao> Transacao { get; } = new();

        public EventoDeAuditoria? EventoRegistrado { get; private set; }

        public CriarPontoHandler CriarHandlerDeCriacao()
        {
            return new CriarPontoHandler(
                Contexto,
                Auditoria.Object,
                UnidadeDeTrabalho.Object,
                new RelogioFixo(),
                new CriarPontoValidator(),
                Pontos.Object);
        }

        public AtualizarPontoHandler CriarHandlerDeAtualizacao()
        {
            return new AtualizarPontoHandler(
                Contexto,
                Auditoria.Object,
                UnidadeDeTrabalho.Object,
                new RelogioFixo(),
                new AtualizarPontoValidator(),
                Pontos.Object);
        }

        public AlterarStatusPontoHandler CriarHandlerDeStatus()
        {
            return new AlterarStatusPontoHandler(
                Contexto,
                Auditoria.Object,
                UnidadeDeTrabalho.Object,
                new RelogioFixo(),
                new AlterarStatusPontoValidator(),
                Pontos.Object);
        }

        public RetirarPontoHandler CriarHandlerDeRetirada()
        {
            return new RetirarPontoHandler(
                Contexto,
                Auditoria.Object,
                UnidadeDeTrabalho.Object,
                new RelogioFixo(),
                Pontos.Object);
        }

        public ListarOpcoesDePontosHandler CriarHandlerDeOpcoes()
        {
            return new ListarOpcoesDePontosHandler(Contexto, Pontos.Object);
        }
    }
}
