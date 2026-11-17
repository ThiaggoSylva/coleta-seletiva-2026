using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ColetaSeletiva.Application.Acesso.RegistroDeCidadao;
using ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Retiradas;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.Infrastructure.Tenancy;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ColetaSeletiva.IntegrationTests.Retiradas;

/// <summary>
/// Fluxos de retirada, volumes, painel, CSV e notificações (RF-10–RF-20; CA-06–CA-10).
/// Os testes compartilham um banco SQL Server real e limpam as solicitações abertas
/// antes de cada cenário para não esbarrar no limite por cidadão.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class RetiradasVolumesApiTests
{
    private const string NomeBanco = "ColetaSeletivaRetiradasVolumesApiTests";
    private const string Senha = "SenhaForte!123";
    private const int MunicipioGoiania = 5208707;

    private static readonly DateTimeOffset Agora =
        new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private static readonly TimeZoneInfo FusoDeSaoPaulo =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    private static ApiDeTeste _fabrica = null!;
    private static string _conexao = null!;
    private static Guid _organizacaoAId;
    private static Guid _organizacaoBId;
    private static Guid _adminAId;
    private static Guid _cidadaoAId;
    private static Guid _tipoPapelId;
    private static Guid _tipoVidroId;
    private static Guid _pontoAId;
    private static Guid _pontoBId;
    private static string _emailAdminA = null!;
    private static string _emailAdminB = null!;
    private static string _emailCidadaoA = null!;
    private static string _emailCidadaoB = null!;
    private static DateOnly _dataPadrao;
    private static DateOnly _dataCancelamento;
    private static DateOnly _dataBloqueio;
    private static DateOnly _dataCapacidade;

    [ClassInitialize]
    public static void Inicializar(TestContext _)
    {
        InicializarAsync().GetAwaiter().GetResult();
    }

    [ClassCleanup]
    public static void Limpar()
    {
        LimparAsync().GetAwaiter().GetResult();
    }

    [TestInitialize]
    public async Task PrepararCenario()
    {
        _dataPadrao = HojeLocal().AddDays(5);
        _dataCancelamento = HojeLocal().AddDays(7);
        _dataBloqueio = HojeLocal().AddDays(9);
        _dataCapacidade = HojeLocal().AddDays(11);

        await RemoverSolicitacoesAbertasAsync();
    }

    [TestMethod]
    public async Task Fluxo_de_retirada_coleta_volumes_painel_e_csv()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenCidadao = await EntrarAsync(cliente, _emailCidadaoA);
        var tokenAdmin = await EntrarAsync(cliente, _emailAdminA);

        var criada = await CriarRetiradaAsync(
            cliente,
            tokenCidadao,
            _dataPadrao,
            ("Manha", 2.5m, _tipoPapelId),
            ("Manha", 1m, _tipoVidroId));
        var id = criada.Id;

        Assert.AreEqual("Solicitada", criada.Detalhe.GetProperty("estado").GetString());
        Assert.IsFalse(criada.Detalhe.GetProperty("dadosPessoaisOcultos").GetBoolean());
        Assert.AreEqual("Ana Souza", criada.Detalhe.GetProperty("nomeCidadao").GetString());

        var notificacoesEquipe = await ListarNotificacoesAsync(
            cliente,
            tokenAdmin,
            apenasNaoLidas: true);
        Assert.IsTrue(
            ContemNotificacao(notificacoesEquipe, id, "SolicitacaoCriada"),
            "A equipe deve ser notificada da nova solicitação.");

        var agendada = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/retiradas/{id}/agendar",
            tokenAdmin,
            new
            {
                data = _dataPadrao.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                periodo = "Manha",
            });
        AssertStatus(HttpStatusCode.OK, agendada);
        using (var corpo = JsonDocument.Parse(await agendada.Content.ReadAsStringAsync()))
        {
            Assert.AreEqual("Agendada", corpo.RootElement.GetProperty("estado").GetString());
        }

        var notificacoesCidadao = await ListarNotificacoesAsync(
            cliente,
            tokenCidadao,
            apenasNaoLidas: true);
        Assert.IsTrue(
            ContemNotificacao(notificacoesCidadao, id, "RetiradaAgendada"),
            "O cidadão deve ser notificado do agendamento.");

        var executada = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/retiradas/{id}/executar",
            tokenAdmin,
            new
            {
                itens = new[]
                {
                    new { tipoResiduoId = _tipoPapelId, quantidade = 12.347m },
                    new { tipoResiduoId = _tipoVidroId, quantidade = 0.813m },
                },
            });
        AssertStatus(HttpStatusCode.OK, executada);

        using (var corpo = JsonDocument.Parse(await executada.Content.ReadAsStringAsync()))
        {
            var raiz = corpo.RootElement;
            Assert.AreEqual("Coletada", raiz.GetProperty("estado").GetString());
            Assert.IsTrue(raiz.GetProperty("dadosPessoaisOcultos").GetBoolean());
            Assert.AreEqual(JsonValueKind.Null, raiz.GetProperty("nomeCidadao").ValueKind);
            Assert.AreEqual(
                2,
                raiz.GetProperty("volumes").GetArrayLength(),
                "A execução deve registrar um volume por tipo informado.");
        }

        var detalheCidadao = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            $"/api/v1/retiradas/minhas/{id}",
            tokenCidadao);
        AssertStatus(HttpStatusCode.OK, detalheCidadao);
        using (var corpo = JsonDocument.Parse(await detalheCidadao.Content.ReadAsStringAsync()))
        {
            Assert.AreEqual("Coletada", corpo.RootElement.GetProperty("estado").GetString());
            Assert.AreEqual("Ana Souza", corpo.RootElement.GetProperty("nomeCidadao").GetString());
        }

        var fila = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            "/api/v1/retiradas?estado=Coletada&page=1&pageSize=50",
            tokenAdmin);
        AssertStatus(HttpStatusCode.OK, fila);
        using (var corpo = JsonDocument.Parse(await fila.Content.ReadAsStringAsync()))
        {
            Assert.IsTrue(
                ContemId(corpo.RootElement.GetProperty("itens"), id),
                "A fila da organização deve listar a solicitação coletada.");
        }

        var volumes = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            "/api/v1/volumes?page=1&pageSize=100",
            tokenAdmin);
        AssertStatus(HttpStatusCode.OK, volumes);

        DateOnly dataLocal;
        using (var corpo = JsonDocument.Parse(await volumes.Content.ReadAsStringAsync()))
        {
            var itens = corpo.RootElement.GetProperty("itens").EnumerateArray().ToList();
            var daSolicitacao = itens
                .Where(item => item.GetProperty("solicitacaoId").ValueKind == JsonValueKind.String
                    && item.GetProperty("solicitacaoId").GetGuid() == id)
                .ToList();
            Assert.AreEqual(2, daSolicitacao.Count, "Os dois volumes da retirada devem aparecer.");
            Assert.IsTrue(
                daSolicitacao.All(item => item.GetProperty("origem").GetString() == "Retirada"));
            dataLocal = DateOnly.Parse(
                daSolicitacao[0].GetProperty("dataLocal").GetString()!,
                CultureInfo.InvariantCulture);
        }

        var consultaDe = dataLocal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var consultaAte = _dataPadrao.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var resumo = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            $"/api/v1/painel/resumo?de={consultaDe}&ate={consultaAte}",
            tokenAdmin);
        AssertStatus(HttpStatusCode.OK, resumo);
        using (var corpo = JsonDocument.Parse(await resumo.Content.ReadAsStringAsync()))
        {
            var raiz = corpo.RootElement;
            var papel = raiz.GetProperty("porTipo")
                .EnumerateArray()
                .Single(item => item.GetProperty("tipoResiduoId").GetGuid() == _tipoPapelId);
            Assert.IsTrue(
                papel.GetProperty("quantidade").GetDecimal() >= 12.347m,
                "O painel deve somar o volume coletado do tipo.");
            Assert.IsTrue(
                raiz.GetProperty("porOrigem")
                    .EnumerateArray()
                    .Any(item => item.GetProperty("origem").GetString() == "Retirada"));
            Assert.IsTrue(
                raiz.GetProperty("retiradasPorEstado")
                    .EnumerateArray()
                    .Any(item => item.GetProperty("estado").GetString() == "Coletada"));
        }

        var serie = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            $"/api/v1/painel/serie?de={consultaDe}&ate={consultaAte}&agrupamento=dia",
            tokenAdmin);
        AssertStatus(HttpStatusCode.OK, serie);
        using (var corpo = JsonDocument.Parse(await serie.Content.ReadAsStringAsync()))
        {
            Assert.AreEqual("dia", corpo.RootElement.GetProperty("agrupamento").GetString());
            Assert.IsTrue(corpo.RootElement.GetProperty("pontos").GetArrayLength() >= 1);
        }

        var csv = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            $"/api/v1/relatorios/volumes.csv?de={consultaDe}&ate={consultaAte}",
            tokenAdmin);
        AssertStatus(HttpStatusCode.OK, csv);
        Assert.IsTrue(
            csv.Content.Headers.ContentType?.MediaType == "text/csv",
            "O relatório deve ser servido como text/csv.");

        var bytes = await csv.Content.ReadAsByteArrayAsync();
        CollectionAssert.AreEqual(
            Encoding.UTF8.GetPreamble(),
            bytes.Take(Encoding.UTF8.GetPreamble().Length).ToArray(),
            "O CSV deve começar com BOM UTF-8.");

        var texto = Encoding.UTF8.GetString(bytes);
        StringAssert.Contains(texto, "Data;Tipo;Unidade;Quantidade;Sinal;Origem;");
        StringAssert.Contains(texto, "12,347");
        StringAssert.Contains(texto, "0,813");
        StringAssert.Contains(texto, ";Retirada;");
    }

    [TestMethod]
    public async Task Cidadao_cancela_e_janela_devolve_a_vaga()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenCidadao = await EntrarAsync(cliente, _emailCidadaoA);

        var criada = await CriarRetiradaAsync(
            cliente,
            tokenCidadao,
            _dataCancelamento,
            ("Manha", 1m, _tipoPapelId));

        var antes = await ObterJanelaAsync(cliente, tokenCidadao, _dataCancelamento, "Manha");
        Assert.AreEqual(10, antes.GetProperty("capacidade").GetInt32());
        Assert.IsTrue(antes.GetProperty("ocupacao").GetInt32() >= 1);

        var cancelada = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/retiradas/minhas/{criada.Id}/cancelar",
            tokenCidadao);
        AssertStatus(HttpStatusCode.OK, cancelada);
        using (var corpo = JsonDocument.Parse(await cancelada.Content.ReadAsStringAsync()))
        {
            Assert.AreEqual("Cancelada", corpo.RootElement.GetProperty("estado").GetString());
        }

        var depois = await ObterJanelaAsync(cliente, tokenCidadao, _dataCancelamento, "Manha");
        Assert.AreEqual(
            antes.GetProperty("ocupacao").GetInt32() - 1,
            depois.GetProperty("ocupacao").GetInt32(),
            "O cancelamento deve liberar a vaga da janela.");

        var repetido = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/retiradas/minhas/{criada.Id}/cancelar",
            tokenCidadao);
        Assert.AreEqual(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.AreEqual(CodigosDeErro.EstadoInvalido, await LerCodigoAsync(repetido));
    }

    [TestMethod]
    public async Task Cancelamento_dentro_de_24_horas_e_recusado()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenCidadao = await EntrarAsync(cliente, _emailCidadaoA);

        var solicitacao = SolicitacaoDeRetirada.Criar(
            _organizacaoAId,
            _cidadaoAId,
            "Ana Souza",
            "(62) 99999-0000",
            "GO",
            MunicipioGoiania,
            "Goiânia",
            "Setor Bueno",
            "Rua das Flores",
            "123",
            null,
            "74000000",
            null,
            null,
            HojeLocal(),
            PeriodoDeRetirada.Manha,
            [(_tipoPapelId, 1m)],
            _adminAId,
            DateTimeOffset.UtcNow.AddDays(-2));

        await using (var contexto = CriarContexto())
        {
            contexto.SolicitacoesDeRetirada.Add(solicitacao);
            await contexto.SaveChangesAsync();
        }

        var resposta = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/retiradas/minhas/{solicitacao.Id}/cancelar",
            tokenCidadao);

        Assert.AreEqual(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.AreEqual(CodigosDeErro.EstadoInvalido, await LerCodigoAsync(resposta));
    }

    [TestMethod]
    public async Task Retiradas_ficam_isoladas_entre_organizacoes_e_cidadaos()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenCidadaoA = await EntrarAsync(cliente, _emailCidadaoA);
        var tokenCidadaoB = await EntrarAsync(cliente, _emailCidadaoB);
        var tokenAdminA = await EntrarAsync(cliente, _emailAdminA);
        var tokenAdminB = await EntrarAsync(cliente, _emailAdminB);

        var criada = await CriarRetiradaAsync(
            cliente,
            tokenCidadaoA,
            _dataPadrao,
            ("Tarde", 1m, _tipoPapelId));

        var outroCidadao = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            $"/api/v1/retiradas/minhas/{criada.Id}",
            tokenCidadaoB);
        Assert.AreEqual(HttpStatusCode.NotFound, outroCidadao.StatusCode);

        var cancelamentoCruzado = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/retiradas/minhas/{criada.Id}/cancelar",
            tokenCidadaoB);
        Assert.AreEqual(HttpStatusCode.NotFound, cancelamentoCruzado.StatusCode);

        var outraOrganizacao = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            $"/api/v1/retiradas/{criada.Id}",
            tokenAdminB);
        Assert.AreEqual(HttpStatusCode.NotFound, outraOrganizacao.StatusCode);

        var agendamentoCruzado = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/retiradas/{criada.Id}/agendar",
            tokenAdminB,
            new
            {
                data = _dataPadrao.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                periodo = "Tarde",
            });
        Assert.AreEqual(HttpStatusCode.NotFound, agendamentoCruzado.StatusCode);

        var filaB = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            "/api/v1/retiradas?page=1&pageSize=50",
            tokenAdminB);
        AssertStatus(HttpStatusCode.OK, filaB);
        using (var corpo = JsonDocument.Parse(await filaB.Content.ReadAsStringAsync()))
        {
            Assert.IsFalse(
                ContemId(corpo.RootElement.GetProperty("itens"), criada.Id),
                "A fila da outra organização não pode expor a solicitação.");
        }

        var propria = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            $"/api/v1/retiradas/{criada.Id}",
            tokenAdminA);
        AssertStatus(HttpStatusCode.OK, propria);
        using (var corpo = JsonDocument.Parse(await propria.Content.ReadAsStringAsync()))
        {
            Assert.AreEqual("Ana Souza", corpo.RootElement.GetProperty("nomeCidadao").GetString());
            Assert.IsFalse(corpo.RootElement.GetProperty("dadosPessoaisOcultos").GetBoolean());
        }

        var cidadaoNaFilaDaEquipe = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            "/api/v1/retiradas?page=1&pageSize=20",
            tokenCidadaoA);
        Assert.AreEqual(HttpStatusCode.Forbidden, cidadaoNaFilaDaEquipe.StatusCode);

        var adminCriando = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            "/api/v1/retiradas",
            tokenAdminA,
            CorpoDeRetirada(_dataPadrao, "Manha"));
        Assert.AreEqual(HttpStatusCode.Forbidden, adminCriando.StatusCode);
    }

    [TestMethod]
    public async Task Recusa_e_nao_realizada_encerram_com_motivo_e_notificam()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenCidadao = await EntrarAsync(cliente, _emailCidadaoA);
        var tokenAdmin = await EntrarAsync(cliente, _emailAdminA);

        var recusavel = await CriarRetiradaAsync(
            cliente,
            tokenCidadao,
            _dataPadrao,
            ("Manha", 1m, _tipoPapelId));

        var recusada = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/retiradas/{recusavel.Id}/recusar",
            tokenAdmin,
            new { motivo = "Fora da área de atendimento." });
        AssertStatus(HttpStatusCode.OK, recusada);
        using (var corpo = JsonDocument.Parse(await recusada.Content.ReadAsStringAsync()))
        {
            Assert.AreEqual("Recusada", corpo.RootElement.GetProperty("estado").GetString());
            Assert.IsTrue(corpo.RootElement.GetProperty("dadosPessoaisOcultos").GetBoolean());
            Assert.AreEqual(
                "Fora da área de atendimento.",
                corpo.RootElement.GetProperty("motivo").GetString());
        }

        var notificacoesCidadao = await ListarNotificacoesAsync(
            cliente,
            tokenCidadao,
            apenasNaoLidas: true);
        Assert.IsTrue(ContemNotificacao(notificacoesCidadao, recusavel.Id, "RetiradaRecusada"));

        var cancelamentoAposRecusa = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/retiradas/minhas/{recusavel.Id}/cancelar",
            tokenCidadao);
        Assert.AreEqual(HttpStatusCode.Conflict, cancelamentoAposRecusa.StatusCode);

        var agendavel = await CriarRetiradaAsync(
            cliente,
            tokenCidadao,
            _dataPadrao,
            ("Tarde", 1m, _tipoVidroId));
        var agendada = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/retiradas/{agendavel.Id}/agendar",
            tokenAdmin,
            new
            {
                data = _dataPadrao.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                periodo = "Tarde",
            });
        AssertStatus(HttpStatusCode.OK, agendada);

        var invalida = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/retiradas/{agendavel.Id}/nao-realizada",
            tokenAdmin,
            new { motivo = "MotivoInventado" });
        Assert.AreEqual(HttpStatusCode.BadRequest, invalida.StatusCode);

        var naoRealizada = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/retiradas/{agendavel.Id}/nao-realizada",
            tokenAdmin,
            new { motivo = "CidadaoAusente" });
        AssertStatus(HttpStatusCode.OK, naoRealizada);
        using (var corpo = JsonDocument.Parse(await naoRealizada.Content.ReadAsStringAsync()))
        {
            Assert.AreEqual("NaoRealizada", corpo.RootElement.GetProperty("estado").GetString());
            Assert.AreEqual("CidadaoAusente", corpo.RootElement.GetProperty("motivo").GetString());
        }

        var notificacoes = await ListarNotificacoesAsync(
            cliente,
            tokenCidadao,
            apenasNaoLidas: true);
        Assert.IsTrue(ContemNotificacao(notificacoes, agendavel.Id, "RetiradaNaoRealizada"));
    }

    [TestMethod]
    public async Task Bloqueio_de_data_impede_solicitacao_e_fica_isolado()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenCidadao = await EntrarAsync(cliente, _emailCidadaoA);
        var tokenAdminA = await EntrarAsync(cliente, _emailAdminA);
        var tokenAdminB = await EntrarAsync(cliente, _emailAdminB);

        var bloqueio = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            "/api/v1/bloqueios-retirada",
            tokenAdminA,
            new
            {
                data = _dataBloqueio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                motivo = "Feriado municipal",
            });
        AssertStatus(HttpStatusCode.Created, bloqueio);

        Guid bloqueioId;
        using (var corpo = JsonDocument.Parse(await bloqueio.Content.ReadAsStringAsync()))
        {
            bloqueioId = corpo.RootElement.GetProperty("id").GetGuid();
        }

        var duplicado = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            "/api/v1/bloqueios-retirada",
            tokenAdminA,
            new
            {
                data = _dataBloqueio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            });
        Assert.AreEqual(HttpStatusCode.Conflict, duplicado.StatusCode);
        Assert.AreEqual(CodigosDeErro.DataBloqueada, await LerCodigoAsync(duplicado));

        var tentativa = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            "/api/v1/retiradas",
            tokenCidadao,
            CorpoDeRetirada(_dataBloqueio, "Manha"));
        Assert.AreEqual(HttpStatusCode.Conflict, tentativa.StatusCode);
        Assert.AreEqual(CodigosDeErro.DataBloqueada, await LerCodigoAsync(tentativa));

        var listaB = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            "/api/v1/bloqueios-retirada",
            tokenAdminB);
        AssertStatus(HttpStatusCode.OK, listaB);
        using (var corpo = JsonDocument.Parse(await listaB.Content.ReadAsStringAsync()))
        {
            Assert.IsFalse(
                corpo.RootElement.EnumerateArray()
                    .Any(item => item.GetProperty("id").GetGuid() == bloqueioId),
                "Bloqueios de outra organização não devem vazar.");
        }

        var remocao = await EnviarAsync(
            cliente,
            HttpMethod.Delete,
            $"/api/v1/bloqueios-retirada/{bloqueioId}",
            tokenAdminA);
        Assert.AreEqual(HttpStatusCode.NoContent, remocao.StatusCode);

        var liberada = await CriarRetiradaAsync(
            cliente,
            tokenCidadao,
            _dataBloqueio,
            ("Manha", 1m, _tipoPapelId));
        Assert.AreEqual(_dataBloqueio, liberada.Data);
    }

    [TestMethod]
    public async Task Capacidade_esgotada_impede_nova_solicitacao_e_remove_janela()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenCidadao = await EntrarAsync(cliente, _emailCidadaoA);
        var tokenAdmin = await EntrarAsync(cliente, _emailAdminA);

        var dia = _dataCapacidade.DayOfWeek;
        await DefinirDisponibilidadeAsync(cliente, tokenAdmin, dia, PeriodoDeRetirada.Tarde, 1);

        try
        {
            await CriarRetiradaAsync(
                cliente,
                tokenCidadao,
                _dataCapacidade,
                ("Tarde", 1m, _tipoPapelId));

            var excedente = await EnviarAsync(
                cliente,
                HttpMethod.Post,
                "/api/v1/retiradas",
                tokenCidadao,
                CorpoDeRetirada(_dataCapacidade, "Tarde"));
            Assert.AreEqual(HttpStatusCode.Conflict, excedente.StatusCode);
            Assert.AreEqual(CodigosDeErro.CapacidadeEsgotada, await LerCodigoAsync(excedente));

            var janelas = await EnviarAsync(
                cliente,
                HttpMethod.Get,
                $"/api/v1/retiradas/janelas?organizacaoId={_organizacaoAId}",
                tokenCidadao);
            AssertStatus(HttpStatusCode.OK, janelas);
            using var corpo = JsonDocument.Parse(await janelas.Content.ReadAsStringAsync());
            Assert.IsFalse(
                corpo.RootElement.EnumerateArray().Any(item =>
                    DateOnly.Parse(
                        item.GetProperty("data").GetString()!,
                        CultureInfo.InvariantCulture) == _dataCapacidade
                    && item.GetProperty("periodo").GetString() == "Tarde"),
                "Janela sem vagas não deve ser oferecida ao cidadão.");
        }
        finally
        {
            await DefinirDisponibilidadeAsync(cliente, tokenAdmin, dia, PeriodoDeRetirada.Tarde, 10);
        }
    }

    [TestMethod]
    public async Task Volumes_aceitam_recebimento_e_ajuste_sem_total_negativo()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenCidadao = await EntrarAsync(cliente, _emailCidadaoA);
        var tokenAdminA = await EntrarAsync(cliente, _emailAdminA);
        var tokenAdminB = await EntrarAsync(cliente, _emailAdminB);

        var recebimento = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            "/api/v1/volumes/recebimentos",
            tokenAdminA,
            new
            {
                pontoId = _pontoAId,
                tipoResiduoId = _tipoPapelId,
                quantidade = 5m,
            });
        AssertStatus(HttpStatusCode.Created, recebimento);

        Guid volumeId;
        using (var corpo = JsonDocument.Parse(await recebimento.Content.ReadAsStringAsync()))
        {
            var raiz = corpo.RootElement;
            volumeId = raiz.GetProperty("id").GetGuid();
            Assert.AreEqual("RecebimentoEmPonto", raiz.GetProperty("origem").GetString());
            Assert.AreEqual("Ponto A Centro", raiz.GetProperty("pontoNome").GetString());
            Assert.AreEqual(5m, raiz.GetProperty("quantidade").GetDecimal());
            Assert.IsFalse(string.IsNullOrWhiteSpace(raiz.GetProperty("unidade").GetString()));
        }

        var ajuste = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/volumes/{volumeId}/ajustes",
            tokenAdminA,
            new { quantidade = -1.5m, motivo = "Correção de pesagem" });
        AssertStatus(HttpStatusCode.Created, ajuste);

        Guid ajusteId;
        using (var corpo = JsonDocument.Parse(await ajuste.Content.ReadAsStringAsync()))
        {
            var raiz = corpo.RootElement;
            ajusteId = raiz.GetProperty("id").GetGuid();
            Assert.AreEqual("Ajuste", raiz.GetProperty("origem").GetString());
            Assert.AreEqual(volumeId, raiz.GetProperty("registroOriginalId").GetGuid());
            Assert.AreEqual(3.5m, raiz.GetProperty("totalDoOriginal").GetDecimal());
            Assert.AreEqual("Correção de pesagem", raiz.GetProperty("motivo").GetString());
        }

        var negativo = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/volumes/{volumeId}/ajustes",
            tokenAdminA,
            new { quantidade = -10m, motivo = "Erro de digitação" });
        Assert.AreEqual(HttpStatusCode.Conflict, negativo.StatusCode);
        Assert.AreEqual(CodigosDeErro.VolumeNegativo, await LerCodigoAsync(negativo));

        var ajusteDeAjuste = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/volumes/{ajusteId}/ajustes",
            tokenAdminA,
            new { quantidade = 1m, motivo = "Tentativa inválida" });
        Assert.AreEqual(HttpStatusCode.Conflict, ajusteDeAjuste.StatusCode);
        Assert.AreEqual(CodigosDeErro.RegistroImutavel, await LerCodigoAsync(ajusteDeAjuste));

        var pontoDeOutraOrganizacao = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            "/api/v1/volumes/recebimentos",
            tokenAdminA,
            new
            {
                pontoId = _pontoBId,
                tipoResiduoId = _tipoPapelId,
                quantidade = 1m,
            });
        Assert.AreEqual(HttpStatusCode.NotFound, pontoDeOutraOrganizacao.StatusCode);

        var futuro = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            "/api/v1/volumes/recebimentos",
            tokenAdminA,
            new
            {
                pontoId = _pontoAId,
                tipoResiduoId = _tipoPapelId,
                quantidade = 1m,
                ocorridoEmUtc = DateTimeOffset.UtcNow.AddDays(1),
            });
        Assert.AreEqual(HttpStatusCode.BadRequest, futuro.StatusCode);

        var ajustes = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            "/api/v1/volumes?origem=Ajuste&page=1&pageSize=50",
            tokenAdminA);
        AssertStatus(HttpStatusCode.OK, ajustes);
        using (var corpo = JsonDocument.Parse(await ajustes.Content.ReadAsStringAsync()))
        {
            Assert.IsTrue(
                ContemId(corpo.RootElement.GetProperty("itens"), ajusteId),
                "O filtro por origem deve devolver o ajuste.");
        }

        var listaB = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            "/api/v1/volumes?page=1&pageSize=100",
            tokenAdminB);
        AssertStatus(HttpStatusCode.OK, listaB);
        using (var corpo = JsonDocument.Parse(await listaB.Content.ReadAsStringAsync()))
        {
            Assert.IsFalse(
                ContemId(corpo.RootElement.GetProperty("itens"), volumeId),
                "Volumes da organização A não podem aparecer para a organização B.");
            Assert.IsFalse(ContemId(corpo.RootElement.GetProperty("itens"), ajusteId));
        }

        var cidadao = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            "/api/v1/volumes?page=1&pageSize=20",
            tokenCidadao);
        Assert.AreEqual(HttpStatusCode.Forbidden, cidadao.StatusCode);
    }

    [TestMethod]
    public async Task Notificacoes_marcam_leitura_e_nao_vazam_entre_usuarios()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenCidadao = await EntrarAsync(cliente, _emailCidadaoA);
        var tokenAdminA = await EntrarAsync(cliente, _emailAdminA);
        var tokenAdminB = await EntrarAsync(cliente, _emailAdminB);

        var criada = await CriarRetiradaAsync(
            cliente,
            tokenCidadao,
            _dataPadrao,
            ("Manha", 1m, _tipoPapelId));

        var antes = await ListarNotificacoesAsync(cliente, tokenAdminA, apenasNaoLidas: true);
        var notificacao = EncontrarNotificacao(antes, criada.Id, "SolicitacaoCriada");
        var naoLidasAntes = antes.NaoLidas;
        Assert.IsTrue(naoLidasAntes >= 1);

        var tentativaCruzada = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/notificacoes/{notificacao.Id}/lida",
            tokenAdminB);
        Assert.AreEqual(HttpStatusCode.NotFound, tentativaCruzada.StatusCode);

        var leitura = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/notificacoes/{notificacao.Id}/lida",
            tokenAdminA);
        AssertStatus(HttpStatusCode.OK, leitura);
        using (var corpo = JsonDocument.Parse(await leitura.Content.ReadAsStringAsync()))
        {
            Assert.IsTrue(corpo.RootElement.GetProperty("lida").GetBoolean());
        }

        var depois = await ListarNotificacoesAsync(cliente, tokenAdminA, apenasNaoLidas: true);
        Assert.AreEqual(naoLidasAntes - 1, depois.NaoLidas);
        Assert.IsFalse(
            depois.Itens.Any(item => item.Id == notificacao.Id),
            "Notificação lida não deve aparecer no filtro de não lidas.");

        var cidadao = await ListarNotificacoesAsync(cliente, tokenCidadao, apenasNaoLidas: true);
        Assert.IsFalse(
            cidadao.Itens.Any(item => item.Id == notificacao.Id),
            "Notificação da equipe não pode vazar para o cidadão.");

        var marcarTodas = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            "/api/v1/notificacoes/lidas",
            tokenAdminA);
        AssertStatus(HttpStatusCode.OK, marcarTodas);

        var zeradas = await ListarNotificacoesAsync(cliente, tokenAdminA, apenasNaoLidas: true);
        Assert.AreEqual(0, zeradas.NaoLidas);
    }

    [TestMethod]
    public async Task Organizacoes_elegiveis_exigem_publicacao_e_vinculo_de_municipio()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenCidadao = await EntrarAsync(cliente, _emailCidadaoA);

        var resposta = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            $"/api/v1/retiradas/organizacoes?municipioId={MunicipioGoiania}",
            tokenCidadao);
        AssertStatus(HttpStatusCode.OK, resposta);

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var ids = corpo.RootElement.EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToList();

        CollectionAssert.Contains(ids, _organizacaoAId);
        Assert.IsFalse(ids.Contains(_organizacaoBId), "Organização de outro município não é elegível.");

        var daOrganizacao = corpo.RootElement.EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == _organizacaoAId);
        Assert.IsTrue(daOrganizacao.GetProperty("tipos").GetArrayLength() >= 2);
    }

    private static async Task InicializarAsync()
    {
        _conexao = await SqlServerTestDatabase.ObterConnectionStringAsync(NomeBanco);

        await using (var contexto = CriarContexto())
        {
            await contexto.Database.EnsureDeletedAsync();
            await contexto.Database.MigrateAsync();
        }

        _fabrica = new ApiDeTeste(
            _conexao,
            new Dictionary<string, string?>
            {
                ["LimitesDeTaxa:Login:PermitLimit"] = "1000",
                ["LimitesDeTaxa:Refresh:PermitLimit"] = "1000",
                ["LimitesDeTaxa:Cadastro:PermitLimit"] = "1000",
            });
        _ = _fabrica.Services;

        await using (var escopo = _fabrica.Services.CreateAsyncScope())
        {
            var registrarOrganizacao = escopo.ServiceProvider
                .GetRequiredService<RegistrarOrganizacaoHandler>();
            var registrarCidadao = escopo.ServiceProvider
                .GetRequiredService<RegistrarCidadaoHandler>();

            var sufixo = Guid.NewGuid().ToString("N");
            _emailAdminA = $"admin.a.{sufixo}@example.org";
            _emailAdminB = $"admin.b.{sufixo}@example.org";
            _emailCidadaoA = $"cidadao.a.{sufixo}@example.org";
            _emailCidadaoB = $"cidadao.b.{sufixo}@example.org";

            var organizacaoA = await registrarOrganizacao.ExecutarAsync(
                new RegistroDeOrganizacaoEntrada(
                    NomeOrganizacao: $"Cooperativa Alfa {sufixo}",
                    Tipo: TipoOrganizacao.Cooperativa,
                    Usuario: _emailAdminA,
                    Email: _emailAdminA,
                    Senha: Senha),
                Guid.NewGuid());
            AssertarSucesso(organizacaoA.Errors, "registro da organização A");
            _organizacaoAId = organizacaoA.Value.OrganizacaoId;
            _adminAId = organizacaoA.Value.AdministradorId;

            var organizacaoB = await registrarOrganizacao.ExecutarAsync(
                new RegistroDeOrganizacaoEntrada(
                    NomeOrganizacao: $"Associação Beta {sufixo}",
                    Tipo: TipoOrganizacao.Associacao,
                    Usuario: _emailAdminB,
                    Email: _emailAdminB,
                    Senha: Senha),
                Guid.NewGuid());
            AssertarSucesso(organizacaoB.Errors, "registro da organização B");
            _organizacaoBId = organizacaoB.Value.OrganizacaoId;

            var cidadaoA = await registrarCidadao.ExecutarAsync(
                new RegistroDeCidadaoEntrada(
                    Nome: "Ana Souza",
                    Email: _emailCidadaoA,
                    Telefone: "(62) 99999-0000",
                    Senha: Senha),
                Guid.NewGuid());
            AssertarSucesso(cidadaoA.Errors, "registro do cidadão A");
            _cidadaoAId = cidadaoA.Value.UsuarioId;

            var cidadaoB = await registrarCidadao.ExecutarAsync(
                new RegistroDeCidadaoEntrada(
                    Nome: "Bruno Lima",
                    Email: _emailCidadaoB,
                    Telefone: "(62) 98888-0000",
                    Senha: Senha),
                Guid.NewGuid());
            AssertarSucesso(cidadaoB.Errors, "registro do cidadão B");
        }

        await using (var contexto = CriarContexto())
        {
            var tipos = await contexto.TiposResiduo
                .AsNoTracking()
                .OrderBy(tipo => tipo.Codigo)
                .Take(2)
                .ToListAsync();
            Assert.AreEqual(2, tipos.Count, "O catálogo precisa ter ao menos dois tipos de resíduo.");
            _tipoPapelId = tipos[0].Id;
            _tipoVidroId = tipos[1].Id;

            var organizacaoA = await contexto.Organizacoes.FirstAsync(
                organizacao => organizacao.Id == _organizacaoAId);
            var organizacaoB = await contexto.Organizacoes.FirstAsync(
                organizacao => organizacao.Id == _organizacaoBId);
            organizacaoA.DefinirPublicacao(true, Agora);
            organizacaoB.DefinirPublicacao(true, Agora);

            var municipioA = OrganizacaoMunicipio.Criar(MunicipioGoiania, Agora);
            ((IEntidadeDaOrganizacao)municipioA).OrganizacaoId = _organizacaoAId;
            var municipioB = OrganizacaoMunicipio.Criar(5201405, Agora);
            ((IEntidadeDaOrganizacao)municipioB).OrganizacaoId = _organizacaoBId;

            var tipoPapelA = OrganizacaoTipo.Criar(_tipoPapelId, true, Agora);
            ((IEntidadeDaOrganizacao)tipoPapelA).OrganizacaoId = _organizacaoAId;
            var tipoVidroA = OrganizacaoTipo.Criar(_tipoVidroId, true, Agora);
            ((IEntidadeDaOrganizacao)tipoVidroA).OrganizacaoId = _organizacaoAId;
            var tipoPapelB = OrganizacaoTipo.Criar(_tipoPapelId, true, Agora);
            ((IEntidadeDaOrganizacao)tipoPapelB).OrganizacaoId = _organizacaoBId;
            var tipoVidroB = OrganizacaoTipo.Criar(_tipoVidroId, true, Agora);
            ((IEntidadeDaOrganizacao)tipoVidroB).OrganizacaoId = _organizacaoBId;

            var pontoA = CriarPonto(
                _organizacaoAId,
                "Ponto A Centro",
                MunicipioGoiania,
                -16.6869,
                -49.2648,
                StatusPonto.Ativo,
                _tipoPapelId,
                _tipoVidroId);
            var pontoB = CriarPonto(
                _organizacaoBId,
                "Ponto B Aparecida",
                5201405,
                -16.8230,
                -49.2200,
                StatusPonto.Ativo,
                _tipoPapelId,
                _tipoVidroId);
            _pontoAId = pontoA.Id;
            _pontoBId = pontoB.Id;

            var disponibilidades = new List<DisponibilidadeDeRetirada>();
            foreach (var dia in Enum.GetValues<DayOfWeek>())
            {
                foreach (var periodo in new[] { PeriodoDeRetirada.Manha, PeriodoDeRetirada.Tarde })
                {
                    var disponibilidadeA = DisponibilidadeDeRetirada.Criar(dia, periodo, 10, Agora);
                    ((IEntidadeDaOrganizacao)disponibilidadeA).OrganizacaoId = _organizacaoAId;
                    disponibilidades.Add(disponibilidadeA);

                    var disponibilidadeB = DisponibilidadeDeRetirada.Criar(dia, periodo, 10, Agora);
                    ((IEntidadeDaOrganizacao)disponibilidadeB).OrganizacaoId = _organizacaoBId;
                    disponibilidades.Add(disponibilidadeB);
                }
            }

            contexto.OrganizacaoMunicipios.AddRange(municipioA, municipioB);
            contexto.OrganizacaoTipos.AddRange(tipoPapelA, tipoVidroA, tipoPapelB, tipoVidroB);
            contexto.PontosDeColeta.AddRange(pontoA, pontoB);
            contexto.DisponibilidadesDeRetirada.AddRange(disponibilidades);
            await contexto.SaveChangesAsync();
        }
    }

    private static async Task LimparAsync()
    {
        if (_fabrica is not null)
        {
            await _fabrica.DisposeAsync();
        }

        await using var contexto = CriarContexto();
        await contexto.Database.EnsureDeletedAsync();
    }

    private static ColetaSeletivaDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<ColetaSeletivaDbContext>()
            .UseSqlServer(_conexao, sqlServer => sqlServer.UseNetTopologySuite())
            .Options;

        return new ColetaSeletivaDbContext(options, new TenantContext());
    }

    private static async Task RemoverSolicitacoesAbertasAsync()
    {
        await using var contexto = CriarContexto();
        var abertas = await contexto.SolicitacoesDeRetirada
            .Where(solicitacao => solicitacao.OrganizacaoId == _organizacaoAId
                && (solicitacao.Estado == EstadoSolicitacao.Solicitada
                    || solicitacao.Estado == EstadoSolicitacao.Agendada))
            .ToListAsync();

        if (abertas.Count == 0)
        {
            return;
        }

        contexto.SolicitacoesDeRetirada.RemoveRange(abertas);
        await contexto.SaveChangesAsync();
    }

    private static async Task<RetiradaCriada> CriarRetiradaAsync(
        HttpClient cliente,
        string token,
        DateOnly data,
        params (string Periodo, decimal Quantidade, Guid TipoResiduoId)[] itens)
    {
        if (itens.Length == 0)
        {
            throw new ArgumentException("Informe ao menos um item.", nameof(itens));
        }

        var resposta = await EnviarAsync(
            cliente,
            HttpMethod.Post,
            "/api/v1/retiradas",
            token,
            CorpoDeRetirada(data, itens[0].Periodo, itens));
        AssertStatus(HttpStatusCode.Created, resposta);

        var detalhe = await LerJsonAsync(resposta);

        return new RetiradaCriada(
            detalhe.GetProperty("id").GetGuid(),
            data,
            detalhe);
    }

    private static object CorpoDeRetirada(
        DateOnly data,
        string periodo,
        params (string Periodo, decimal Quantidade, Guid TipoResiduoId)[] itens)
    {
        var itensDoCorpo = itens.Length == 0
            ? new List<object>
            {
                new { tipoResiduoId = _tipoPapelId, quantidadeEstimada = 1m },
            }
            : itens
                .Select(item => (object)new
                {
                    tipoResiduoId = item.TipoResiduoId,
                    quantidadeEstimada = item.Quantidade,
                })
                .ToList();

        return new
        {
            organizacaoId = _organizacaoAId,
            municipioId = MunicipioGoiania,
            bairro = "Setor Bueno",
            rua = "Rua das Flores",
            numero = "123",
            complemento = (string?)null,
            cep = "74000000",
            pontoReferencia = (string?)null,
            observacoes = (string?)null,
            data = data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            periodo,
            itens = itensDoCorpo,
        };
    }

    private static async Task<JsonElement> ObterJanelaAsync(
        HttpClient cliente,
        string token,
        DateOnly data,
        string periodo)
    {
        var resposta = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            $"/api/v1/retiradas/janelas?organizacaoId={_organizacaoAId}",
            token);
        AssertStatus(HttpStatusCode.OK, resposta);

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var janela = corpo.RootElement.EnumerateArray()
            .FirstOrDefault(item =>
                DateOnly.Parse(item.GetProperty("data").GetString()!, CultureInfo.InvariantCulture) == data
                && item.GetProperty("periodo").GetString() == periodo);

        Assert.AreNotEqual(
            JsonValueKind.Undefined,
            janela.ValueKind,
            $"A janela {data:yyyy-MM-dd} ({periodo}) deve estar disponível.");
        return janela.Clone();
    }

    private static async Task DefinirDisponibilidadeAsync(
        HttpClient cliente,
        string token,
        DayOfWeek diaEspecial,
        PeriodoDeRetirada periodoEspecial,
        int capacidadeEspecial)
    {
        var dias = new List<object>();
        foreach (var dia in Enum.GetValues<DayOfWeek>())
        {
            foreach (var periodo in new[] { PeriodoDeRetirada.Manha, PeriodoDeRetirada.Tarde })
            {
                dias.Add(new
                {
                    diaDaSemana = dia.ToString(),
                    periodo = periodo.ToString(),
                    capacidade = dia == diaEspecial && periodo == periodoEspecial
                        ? capacidadeEspecial
                        : 10,
                });
            }
        }

        var resposta = await EnviarAsync(
            cliente,
            HttpMethod.Put,
            "/api/v1/disponibilidade-retirada",
            token,
            new { dias });
        AssertStatus(HttpStatusCode.OK, resposta);
    }

    private static async Task<ListaDeNotificacoes> ListarNotificacoesAsync(
        HttpClient cliente,
        string token,
        bool apenasNaoLidas)
    {
        var resposta = await EnviarAsync(
            cliente,
            HttpMethod.Get,
            $"/api/v1/notificacoes?apenasNaoLidas={apenasNaoLidas.ToString().ToLowerInvariant()}",
            token);
        AssertStatus(HttpStatusCode.OK, resposta);

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var itens = corpo.RootElement.GetProperty("itens")
            .EnumerateArray()
            .Select(item => new NotificacaoLida(
                item.GetProperty("id").GetGuid(),
                item.GetProperty("tipo").GetString() ?? string.Empty,
                item.GetProperty("referenciaId").GetGuid()))
            .ToList();

        return new ListaDeNotificacoes(
            itens,
            corpo.RootElement.GetProperty("naoLidas").GetInt32());
    }

    private static NotificacaoLida EncontrarNotificacao(
        ListaDeNotificacoes lista,
        Guid referenciaId,
        string tipo)
    {
        var notificacao = lista.Itens.FirstOrDefault(
            item => item.ReferenciaId == referenciaId && item.Tipo == tipo);

        Assert.IsNotNull(notificacao, $"A notificação {tipo} da referência {referenciaId} deve existir.");

        return notificacao!;
    }

    private static bool ContemNotificacao(
        ListaDeNotificacoes lista,
        Guid referenciaId,
        string tipo)
    {
        return lista.Itens.Any(item => item.ReferenciaId == referenciaId && item.Tipo == tipo);
    }

    private static bool ContemId(JsonElement itens, Guid id)
    {
        return itens.EnumerateArray()
            .Any(item => item.GetProperty("id").GetGuid() == id);
    }

    private static async Task<HttpResponseMessage> EnviarAsync(
        HttpClient cliente,
        HttpMethod metodo,
        string caminho,
        string token,
        object? corpo = null)
    {
        var requisicao = new HttpRequestMessage(metodo, caminho);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (corpo is not null)
        {
            requisicao.Content = JsonContent.Create(corpo);
        }

        return await cliente.SendAsync(requisicao);
    }

    private static async Task<string> EntrarAsync(HttpClient cliente, string identificador)
    {
        var resposta = await cliente.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { identificador, senha = Senha });
        AssertStatus(HttpStatusCode.OK, resposta);

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var token = corpo.RootElement.GetProperty("tokenDeAcesso").GetString();

        Assert.IsFalse(string.IsNullOrWhiteSpace(token), "O login deve devolver um token.");

        return token!;
    }

    private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
    {
        var texto = await resposta.Content.ReadAsStringAsync();
        using var documento = JsonDocument.Parse(texto);

        return documento.RootElement.Clone();
    }

    private static async Task<string?> LerCodigoAsync(HttpResponseMessage resposta)
    {
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        return corpo.RootElement.TryGetProperty("code", out var codigo)
            ? codigo.GetString()
            : null;
    }

    private static void AssertStatus(HttpStatusCode esperado, HttpResponseMessage resposta)
    {
        var corpo = resposta.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        Assert.AreEqual(esperado, resposta.StatusCode, corpo);
    }

    private static void AssertarSucesso(
        IEnumerable<FluentResults.IError> erros,
        string contexto)
    {
        var lista = erros.Select(erro => erro.Message).ToList();
        Assert.AreEqual(0, lista.Count, $"Falha no {contexto}: {string.Join("; ", lista)}");
    }

    private static PontoDeColeta CriarPonto(
        Guid organizacaoId,
        string nome,
        int municipioId,
        double latitude,
        double longitude,
        StatusPonto status,
        params Guid[] tiposResiduoIds)
    {
        var ponto = PontoDeColeta.Criar(
            nome,
            municipioId,
            "GO",
            "Centro",
            "Rua de Teste",
            "100",
            null,
            "74000000",
            latitude,
            longitude,
            null,
            status,
            [new HorarioDeFuncionamento(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0))],
            tiposResiduoIds,
            Agora);

        ((IEntidadeDaOrganizacao)ponto).OrganizacaoId = organizacaoId;

        return ponto;
    }

    private static DateOnly HojeLocal()
    {
        return DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, FusoDeSaoPaulo));
    }

    private sealed record RetiradaCriada(Guid Id, DateOnly Data, JsonElement Detalhe);

    private sealed record NotificacaoLida(Guid Id, string Tipo, Guid ReferenciaId);

    private sealed record ListaDeNotificacoes(
        IReadOnlyList<NotificacaoLida> Itens,
        int NaoLidas);
}
