using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ColetaSeletiva.Application;
using ColetaSeletiva.Application.Acesso.RegistroDeCidadao;
using ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Identidade;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.Infrastructure.Tenancy;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ColetaSeletiva.IntegrationTests.F2;

/// <summary>
/// Cobre as rotas da F2 (perfil, catálogo, pontos e consulta pública) em SQL Server real,
/// incluindo isolamento entre organizações e ausência de dados pessoais na resposta pública.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class FuncionalidadesDaF2ApiTests
{
    private const string NomeBanco = "ColetaSeletivaF2ApiTests";
    private const string Senha = "SenhaForte!123";

    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private static string _connectionString = null!;
    private static ApiDeTeste _fabrica = null!;
    private static Guid _organizacaoAId;
    private static Guid _organizacaoBId;
    private static Guid _organizacaoSemVinculosId;
    private static string _adminAUsuario = null!;
    private static string _adminBUsuario = null!;
    private static string _operadorAUsuario = null!;
    private static string _cidadaoEmail = null!;
    private static string _cidadaoNome = null!;
    private static string _cidadaoTelefone = null!;
    private static int _municipioA;
    private static int _municipioB;
    private static int _municipioNaoAtendido;
    private static string _municipioANome = null!;
    private static Guid _tipoPapelId;
    private static Guid _tipoMetalId;

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

    [TestMethod]
    public async Task Perfil_da_organizacao_e_isolado_entre_A_e_B()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenA = await EntrarAsync(cliente, _adminAUsuario);
        var tokenB = await EntrarAsync(cliente, _adminBUsuario);

        var perfilA = await ObterJsonAsync(cliente, "/api/v1/organizacao", tokenA);
        var perfilB = await ObterJsonAsync(cliente, "/api/v1/organizacao", tokenB);

        StringAssert.Contains(perfilA.GetProperty("nome").GetString(), "Organização A");
        Assert.IsTrue(MunicipiosDe(perfilA).Contains(_municipioA));
        Assert.IsFalse(MunicipiosDe(perfilA).Contains(_municipioB));
        Assert.IsTrue(TiposDe(perfilA).Contains(_tipoPapelId));
        Assert.IsFalse(TiposDe(perfilA).Contains(_tipoMetalId));

        StringAssert.Contains(perfilB.GetProperty("nome").GetString(), "Organização B");
        Assert.IsTrue(MunicipiosDe(perfilB).Contains(_municipioB));
        Assert.IsFalse(MunicipiosDe(perfilB).Contains(_municipioA));
        Assert.IsTrue(TiposDe(perfilB).Contains(_tipoMetalId));
        Assert.IsFalse(TiposDe(perfilB).Contains(_tipoPapelId));
    }

    [TestMethod]
    public async Task Administrador_atualiza_somente_o_proprio_perfil()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenB = await EntrarAsync(cliente, _adminBUsuario);
        var nomeAntesDeB = (await ObterJsonAsync(cliente, "/api/v1/organizacao", tokenB))
            .GetProperty("nome")
            .GetString();
        var tokenA = await EntrarAsync(cliente, _adminAUsuario);
        var novoNome = $"Organização A Editada {Guid.NewGuid():N}";

        var requisicao = new HttpRequestMessage(HttpMethod.Put, "/api/v1/organizacao");
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        requisicao.Content = JsonContent.Create(new
        {
            nome = novoNome,
            tipo = "Cooperativa",
            contatoEmail = "contato.a@example.org",
            contatoTelefone = "(62) 3333-4444",
            fuso = "America/Sao_Paulo",
            limiteSolicitacoesAbertas = 15,
        });

        var resposta = await cliente.SendAsync(requisicao);

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode, await resposta.Content.ReadAsStringAsync());
        using (var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()))
        {
            Assert.AreEqual(novoNome, corpo.RootElement.GetProperty("nome").GetString());
            Assert.AreEqual(15, corpo.RootElement.GetProperty("limiteSolicitacoesAbertas").GetInt32());
        }

        var organizacaoEditada = await ObterOrganizacaoAsync(_organizacaoAId);
        Assert.AreEqual(novoNome, organizacaoEditada.Nome);

        var organizacaoIntacta = await ObterOrganizacaoAsync(_organizacaoBId);
        Assert.AreEqual(nomeAntesDeB, organizacaoIntacta.Nome);
    }

    [TestMethod]
    public async Task Gestao_do_perfil_exige_administrador()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenOperador = await EntrarAsync(cliente, _operadorAUsuario);
        var tokenCidadao = await EntrarAsync(cliente, _cidadaoEmail);

        var rotas = new (HttpMethod Metodo, string Caminho)[]
        {
            (HttpMethod.Get, "/api/v1/organizacao"),
            (HttpMethod.Put, "/api/v1/organizacao"),
            (HttpMethod.Put, "/api/v1/organizacao/publicacao"),
            (HttpMethod.Put, "/api/v1/organizacao/municipios"),
            (HttpMethod.Put, "/api/v1/organizacao/periodos"),
            (HttpMethod.Put, "/api/v1/organizacao/tipos"),
        };

        foreach (var rota in rotas)
        {
            foreach (var token in new[] { tokenOperador, tokenCidadao })
            {
                var resposta = await cliente.SendAsync(
                    CriarRequisicao(rota.Metodo, rota.Caminho, token));

                Assert.AreEqual(
                    HttpStatusCode.Forbidden,
                    resposta.StatusCode,
                    $"{rota.Metodo} {rota.Caminho} deveria negar o papel.");
            }

            var anonima = await cliente.SendAsync(
                CriarRequisicao(rota.Metodo, rota.Caminho, token: null));

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                anonima.StatusCode,
                $"{rota.Metodo} {rota.Caminho} deveria exigir autenticação.");
        }
    }

    [TestMethod]
    public async Task Publicacao_exige_municipio_e_tipo_e_pode_ser_desfeita()
    {
        using var cliente = _fabrica.CreateClient();
        var token = await EntrarAsync(cliente, _adminSemVinculosUsuario);
        var caminho = "/api/v1/organizacao/publicacao";

        var semRequisitos = await EnviarAsync(
            cliente,
            HttpMethod.Put,
            caminho,
            token,
            new { publicada = true });
        Assert.AreEqual(HttpStatusCode.Conflict, semRequisitos.StatusCode);
        Assert.AreEqual(
            "publicacao_incompleta",
            await LerCodigoAsync(semRequisitos));

        var municipios = await EnviarAsync(
            cliente,
            HttpMethod.Put,
            "/api/v1/organizacao/municipios",
            token,
            new { municipioIds = new[] { _municipioNaoAtendido } });
        Assert.AreEqual(HttpStatusCode.OK, municipios.StatusCode);
        var tipos = await EnviarAsync(
            cliente,
            HttpMethod.Put,
            "/api/v1/organizacao/tipos",
            token,
            new { tipos = new[] { new { tipoResiduoId = _tipoPapelId, aceitaRetiradaDomiciliar = false } } });
        Assert.AreEqual(HttpStatusCode.OK, tipos.StatusCode);

        var publicacao = await EnviarAsync(
            cliente,
            HttpMethod.Put,
            caminho,
            token,
            new { publicada = true });
        Assert.AreEqual(HttpStatusCode.OK, publicacao.StatusCode);
        using (var corpo = JsonDocument.Parse(await publicacao.Content.ReadAsStringAsync()))
        {
            Assert.IsTrue(corpo.RootElement.GetProperty("publicada").GetBoolean());
        }

        var organizacaoPublicada = await ObterOrganizacaoAsync(_organizacaoSemVinculosId);
        Assert.IsTrue(organizacaoPublicada.Publicada);

        var despublicacao = await EnviarAsync(
            cliente,
            HttpMethod.Put,
            caminho,
            token,
            new { publicada = false });
        Assert.AreEqual(HttpStatusCode.OK, despublicacao.StatusCode);
        using var corpoDespublicacao = JsonDocument.Parse(await despublicacao.Content.ReadAsStringAsync());
        Assert.IsFalse(corpoDespublicacao.RootElement.GetProperty("publicada").GetBoolean());
        Assert.IsFalse((await ObterOrganizacaoAsync(_organizacaoSemVinculosId)).Publicada);
    }

    [TestMethod]
    public async Task Administrador_gerencia_pontos_com_as_validacoes_da_fase()
    {
        using var cliente = _fabrica.CreateClient();
        var token = await EntrarAsync(cliente, _adminAUsuario);
        var nome = $"Ponto A {Guid.NewGuid():N}";

        var criacao = await CriarPontoAsync(cliente, token, nome, _municipioA, _tipoPapelId);
        Assert.AreEqual(HttpStatusCode.Created, criacao.StatusCode, await criacao.Content.ReadAsStringAsync());
        using var corpoCriado = JsonDocument.Parse(await criacao.Content.ReadAsStringAsync());
        var pontoId = corpoCriado.RootElement.GetProperty("id").GetGuid();
        Assert.AreEqual(nome, corpoCriado.RootElement.GetProperty("nome").GetString());

        var nomeRepetido = await CriarPontoAsync(cliente, token, nome, _municipioA, _tipoPapelId);
        Assert.AreEqual(HttpStatusCode.Conflict, nomeRepetido.StatusCode);
        Assert.AreEqual("nome_em_uso", await LerCodigoAsync(nomeRepetido));

        var tipoNaoHabilitado = await CriarPontoAsync(
            cliente,
            token,
            $"{nome} tipo",
            _municipioA,
            _tipoMetalId);
        Assert.AreEqual(HttpStatusCode.BadRequest, tipoNaoHabilitado.StatusCode);
        Assert.IsTrue(await ErrosDeValidacaoContemCampoAsync(tipoNaoHabilitado, "TiposResiduoIds"));

        var municipioNaoAtendido = await CriarPontoAsync(
            cliente,
            token,
            $"{nome} municipio",
            _municipioB,
            _tipoPapelId);
        Assert.AreEqual(HttpStatusCode.BadRequest, municipioNaoAtendido.StatusCode);
        Assert.IsTrue(await ErrosDeValidacaoContemCampoAsync(municipioNaoAtendido, "MunicipioId"));

        var edicao = await EnviarAsync(
            cliente,
            HttpMethod.Put,
            $"/api/v1/pontos/{pontoId}",
            token,
            CorpoDoPonto($"{nome} editado", _municipioA, _tipoPapelId));
        Assert.AreEqual(HttpStatusCode.OK, edicao.StatusCode);

        var status = await EnviarAsync(
            cliente,
            HttpMethod.Put,
            $"/api/v1/pontos/{pontoId}/status",
            token,
            new { status = "Inativo" });
        Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
        using (var corpoStatus = JsonDocument.Parse(await status.Content.ReadAsStringAsync()))
        {
            Assert.AreEqual("Inativo", corpoStatus.RootElement.GetProperty("status").GetString());
            Assert.IsFalse(corpoStatus.RootElement.GetProperty("abertoAgora").GetBoolean());
        }

        var retirada = await cliente.SendAsync(
            CriarRequisicao(HttpMethod.Delete, $"/api/v1/pontos/{pontoId}", token));
        Assert.AreEqual(HttpStatusCode.NoContent, retirada.StatusCode);

        var lista = await ObterJsonAsync(cliente, "/api/v1/pontos", token);
        Assert.IsFalse(IdsDe(lista).Contains(pontoId), "Ponto retirado não deve aparecer na listagem.");

        var nomeReservado = await CriarPontoAsync(
            cliente,
            token,
            $"{nome} editado",
            _municipioA,
            _tipoPapelId);
        Assert.AreEqual(HttpStatusCode.Conflict, nomeReservado.StatusCode);
        Assert.AreEqual("nome_em_uso", await LerCodigoAsync(nomeReservado));
    }

    [TestMethod]
    public async Task Ponto_de_outra_organizacao_nao_e_consultado_nem_editado()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenA = await EntrarAsync(cliente, _adminAUsuario);
        var tokenB = await EntrarAsync(cliente, _adminBUsuario);
        var nome = $"Ponto Isolamento {Guid.NewGuid():N}";

        var criacao = await CriarPontoAsync(cliente, tokenA, nome, _municipioA, _tipoPapelId);
        Assert.AreEqual(HttpStatusCode.Created, criacao.StatusCode);
        using var corpo = JsonDocument.Parse(await criacao.Content.ReadAsStringAsync());
        var pontoId = corpo.RootElement.GetProperty("id").GetGuid();

        var consulta = await cliente.SendAsync(
            CriarRequisicao(HttpMethod.Get, $"/api/v1/pontos/{pontoId}", tokenB));
        Assert.AreEqual(HttpStatusCode.NotFound, consulta.StatusCode);

        var edicao = await EnviarAsync(
            cliente,
            HttpMethod.Put,
            $"/api/v1/pontos/{pontoId}",
            tokenB,
            CorpoDoPonto($"{nome} forjado", _municipioB, _tipoMetalId));
        Assert.AreEqual(HttpStatusCode.NotFound, edicao.StatusCode);

        var retirada = await cliente.SendAsync(
            CriarRequisicao(HttpMethod.Delete, $"/api/v1/pontos/{pontoId}", tokenB));
        Assert.AreEqual(HttpStatusCode.NotFound, retirada.StatusCode);

        var consultaA = await cliente.SendAsync(
            CriarRequisicao(HttpMethod.Get, $"/api/v1/pontos/{pontoId}", tokenA));
        Assert.AreEqual(HttpStatusCode.OK, consultaA.StatusCode);
        using var corpoA = JsonDocument.Parse(await consultaA.Content.ReadAsStringAsync());
        Assert.AreEqual(nome, corpoA.RootElement.GetProperty("nome").GetString());
        Assert.IsFalse(corpoA.RootElement.GetProperty("excluido").GetBoolean());
    }

    [TestMethod]
    public async Task Operador_consulta_opcoes_mas_nao_gerencia_pontos()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenAdmin = await EntrarAsync(cliente, _adminAUsuario);
        var tokenOperador = await EntrarAsync(cliente, _operadorAUsuario);
        var tokenCidadao = await EntrarAsync(cliente, _cidadaoEmail);
        var nome = $"Ponto Operador {Guid.NewGuid():N}";

        var criacao = await CriarPontoAsync(cliente, tokenAdmin, nome, _municipioA, _tipoPapelId);
        Assert.AreEqual(HttpStatusCode.Created, criacao.StatusCode);
        using var corpo = JsonDocument.Parse(await criacao.Content.ReadAsStringAsync());
        var pontoId = corpo.RootElement.GetProperty("id").GetGuid();

        var opcoes = await ObterJsonAsync(cliente, "/api/v1/pontos/opcoes", tokenOperador);
        var idsDasOpcoes = opcoes.EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToList();
        CollectionAssert.Contains(idsDasOpcoes, pontoId);

        var rotasProibidas = new (HttpMethod Metodo, string Caminho, object? Corpo)[]
        {
            (HttpMethod.Get, "/api/v1/pontos", null),
            (HttpMethod.Get, $"/api/v1/pontos/{pontoId}", null),
            (HttpMethod.Post, "/api/v1/pontos", CorpoDoPonto(nome, _municipioA, _tipoPapelId)),
            (HttpMethod.Put, $"/api/v1/pontos/{pontoId}", CorpoDoPonto(nome, _municipioA, _tipoPapelId)),
            (HttpMethod.Put, $"/api/v1/pontos/{pontoId}/status", new { status = "Inativo" }),
            (HttpMethod.Delete, $"/api/v1/pontos/{pontoId}", null),
        };

        foreach (var rota in rotasProibidas)
        {
            foreach (var token in new[] { tokenOperador, tokenCidadao })
            {
                var resposta = await EnviarAsync(
                    cliente,
                    rota.Metodo,
                    rota.Caminho,
                    token,
                    rota.Corpo);

                Assert.AreEqual(
                    HttpStatusCode.Forbidden,
                    resposta.StatusCode,
                    $"{rota.Metodo} {rota.Caminho} deveria negar o papel.");
            }
        }

        var opcoesCidadao = await cliente.SendAsync(
            CriarRequisicao(HttpMethod.Get, "/api/v1/pontos/opcoes", tokenCidadao));
        Assert.AreEqual(HttpStatusCode.Forbidden, opcoesCidadao.StatusCode);

        var consultaA = await cliente.SendAsync(
            CriarRequisicao(HttpMethod.Get, $"/api/v1/pontos/{pontoId}", tokenAdmin));
        Assert.AreEqual(HttpStatusCode.OK, consultaA.StatusCode);
    }

    [TestMethod]
    public async Task Consulta_publica_devolve_pontos_publicados_sem_dados_pessoais()
    {
        using var cliente = _fabrica.CreateClient();
        var token = await EntrarAsync(cliente, _adminAUsuario);
        var nome = $"Ponto Publico {Guid.NewGuid():N}";

        var criacao = await CriarPontoAsync(cliente, token, nome, _municipioA, _tipoPapelId, todasAsHoras: true);
        Assert.AreEqual(HttpStatusCode.Created, criacao.StatusCode);

        var publicacao = await EnviarAsync(
            cliente,
            HttpMethod.Put,
            "/api/v1/organizacao/publicacao",
            token,
            new { publicada = true });
        Assert.AreEqual(HttpStatusCode.OK, publicacao.StatusCode, await publicacao.Content.ReadAsStringAsync());

        var resposta = await cliente.PostAsJsonAsync(
            "/api/v1/publico/pontos/busca",
            new { municipioId = _municipioA, abertoAgora = true });
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode, corpo);
        Assert.IsTrue(
            resposta.Headers.CacheControl?.Public == true,
            "A consulta pública deve ter cache curto público.");

        using var documento = JsonDocument.Parse(corpo);
        var itens = documento.RootElement.GetProperty("itens").EnumerateArray().ToList();
        var item = itens.Single(ponto => ponto.GetProperty("nome").GetString() == nome);
        Assert.AreEqual(
            (await ObterOrganizacaoAsync(_organizacaoAId)).Nome,
            item.GetProperty("organizacaoNome").GetString());
        Assert.IsTrue(item.GetProperty("abertoAgora").GetBoolean());

        Assert.IsFalse(corpo.Contains(_cidadaoNome, StringComparison.Ordinal));
        Assert.IsFalse(corpo.Contains(_cidadaoEmail, StringComparison.Ordinal));
        Assert.IsFalse(corpo.Contains(_cidadaoTelefone, StringComparison.Ordinal));
        Assert.IsFalse(corpo.Contains("cpf", StringComparison.OrdinalIgnoreCase));

        var despublicacao = await EnviarAsync(
            cliente,
            HttpMethod.Put,
            "/api/v1/organizacao/publicacao",
            token,
            new { publicada = false });
        Assert.AreEqual(HttpStatusCode.OK, despublicacao.StatusCode);

        var aposDespublicacao = await cliente.PostAsJsonAsync(
            "/api/v1/publico/pontos/busca",
            new { municipioId = _municipioA });
        using var corpoApos = JsonDocument.Parse(await aposDespublicacao.Content.ReadAsStringAsync());
        var nomes = corpoApos.RootElement.GetProperty("itens")
            .EnumerateArray()
            .Select(ponto => ponto.GetProperty("nome").GetString())
            .ToList();
        CollectionAssert.DoesNotContain(nomes, nome);
    }

    [TestMethod]
    public async Task Catalogo_global_exige_sessao_e_filtra_municipios_por_uf_e_texto()
    {
        using var cliente = _fabrica.CreateClient();
        var token = await EntrarAsync(cliente, _adminAUsuario);

        var semSessao = await cliente.GetAsync("/api/v1/catalogo/tipos");
        Assert.AreEqual(HttpStatusCode.Unauthorized, semSessao.StatusCode);

        var tipos = await ObterJsonAsync(cliente, "/api/v1/catalogo/tipos", token);
        var ids = tipos.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).ToList();
        CollectionAssert.Contains(ids, _tipoPapelId);

        var municipios = await ObterJsonAsync(
            cliente,
            $"/api/v1/catalogo/municipios?uf=GO&texto={Uri.EscapeDataString(_municipioANome)}",
            token);
        var municipio = municipios.EnumerateArray()
            .Single(item => item.GetProperty("id").GetInt32() == _municipioA);
        Assert.AreEqual(_municipioANome, municipio.GetProperty("nome").GetString());
        Assert.AreEqual("GO", municipio.GetProperty("uf").GetString());
    }

    private static string _adminSemVinculosUsuario = null!;

    private static string _adminSemVinculosEmail = null!;

    private static HttpRequestMessage CriarRequisicao(
        HttpMethod metodo,
        string caminho,
        string? token,
        object? corpo = null)
    {
        var requisicao = new HttpRequestMessage(metodo, caminho);

        if (token is not null)
        {
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (corpo is not null)
        {
            requisicao.Content = JsonContent.Create(corpo);
        }

        return requisicao;
    }

    private static async Task<HttpResponseMessage> EnviarAsync(
        HttpClient cliente,
        HttpMethod metodo,
        string caminho,
        string token,
        object? corpo)
    {
        return await cliente.SendAsync(CriarRequisicao(metodo, caminho, token, corpo));
    }

    private static async Task<HttpResponseMessage> CriarPontoAsync(
        HttpClient cliente,
        string token,
        string nome,
        int municipioId,
        Guid tipoResiduoId,
        bool todasAsHoras = false)
    {
        return await EnviarAsync(
            cliente,
            HttpMethod.Post,
            "/api/v1/pontos",
            token,
            CorpoDoPonto(nome, municipioId, tipoResiduoId, todasAsHoras));
    }

    private static object CorpoDoPonto(
        string nome,
        int municipioId,
        Guid tipoResiduoId,
        bool todasAsHoras = false)
    {
        var horarios = todasAsHoras
            ? Enum.GetValues<DayOfWeek>()
                .Select(dia => new { diaDaSemana = dia.ToString(), horaInicio = "00:00", horaFim = "23:59" })
                .ToArray()
            : [new { diaDaSemana = "Monday", horaInicio = "08:00", horaFim = "17:00" }];

        return new
        {
            nome,
            municipioId,
            bairro = "Setor Central",
            rua = "Rua do Ouro",
            numero = "100",
            complemento = (string?)null,
            cep = "74015010",
            latitude = -16.6869,
            longitude = -49.2648,
            observacoes = (string?)null,
            status = "Ativo",
            horarios,
            tiposResiduoIds = new[] { tipoResiduoId },
        };
    }

    private static async Task<JsonElement> ObterJsonAsync(
        HttpClient cliente,
        string caminho,
        string token)
    {
        var resposta = await cliente.SendAsync(CriarRequisicao(HttpMethod.Get, caminho, token));
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode, corpo);

        return JsonDocument.Parse(corpo).RootElement.Clone();
    }

    private static async Task<string> LerCodigoAsync(HttpResponseMessage resposta)
    {
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        return corpo.RootElement.GetProperty("code").GetString()!;
    }

    private static async Task<bool> ErrosDeValidacaoContemCampoAsync(
        HttpResponseMessage resposta,
        string campo)
    {
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        return corpo.RootElement
            .GetProperty("errors")
            .EnumerateObject()
            .Any(propriedade => string.Equals(propriedade.Name, campo, StringComparison.OrdinalIgnoreCase));
    }

    private static List<int> MunicipiosDe(JsonElement perfil)
    {
        return perfil.GetProperty("municipios")
            .EnumerateArray()
            .Select(municipio => municipio.GetProperty("id").GetInt32())
            .ToList();
    }

    private static List<Guid> TiposDe(JsonElement perfil)
    {
        return perfil.GetProperty("tipos")
            .EnumerateArray()
            .Select(tipo => tipo.GetProperty("tipoResiduoId").GetGuid())
            .ToList();
    }

    private static List<Guid> IdsDe(JsonElement lista)
    {
        return lista.EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToList();
    }

    private static async Task<string> EntrarAsync(HttpClient cliente, string identificador)
    {
        var resposta = await cliente.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { identificador, senha = Senha });
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode, corpo);

        using var documento = JsonDocument.Parse(corpo);
        return documento.RootElement.GetProperty("tokenDeAcesso").GetString()!;
    }

    private static async Task<Organizacao> ObterOrganizacaoAsync(Guid organizacaoId)
    {
        await using var contexto = CriarContexto();
        return await contexto.Organizacoes
            .AsNoTracking()
            .SingleAsync(organizacao => organizacao.Id == organizacaoId);
    }

    private static ColetaSeletivaDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<ColetaSeletivaDbContext>()
            .UseSqlServer(_connectionString, sqlServer => sqlServer.UseNetTopologySuite())
            .Options;

        return new ColetaSeletivaDbContext(options, new TenantContext());
    }

    private static async Task InicializarAsync()
    {
        _connectionString = await SqlServerTestDatabase.ObterConnectionStringAsync(NomeBanco);

        await using (var contexto = CriarContexto())
        {
            await contexto.Database.EnsureDeletedAsync();
            await contexto.Database.MigrateAsync();
        }

        await using (var contexto = CriarContexto())
        {
            var tipos = await contexto.TiposResiduo
                .AsNoTracking()
                .OrderBy(tipo => tipo.Codigo)
                .Take(2)
                .ToListAsync();
            Assert.AreEqual(2, tipos.Count, "O catálogo semeado precisa ter ao menos dois tipos.");

            var municipios = await contexto.Municipios
                .AsNoTracking()
                .Where(municipio => municipio.Uf == "GO")
                .OrderBy(municipio => municipio.Id)
                .Take(3)
                .ToListAsync();
            Assert.AreEqual(3, municipios.Count, "O catálogo semeado precisa ter ao menos três municípios.");

            _tipoPapelId = tipos[1].Id;
            _tipoMetalId = tipos[0].Id;
            _municipioA = municipios[0].Id;
            _municipioANome = municipios[0].Nome;
            _municipioB = municipios[1].Id;
            _municipioNaoAtendido = municipios[2].Id;
        }

        _fabrica = new ApiDeTeste(
            _connectionString,
            new Dictionary<string, string?>
            {
                ["LimitesDeTaxa:Login:PermitLimit"] = "1000",
                ["LimitesDeTaxa:Refresh:PermitLimit"] = "1000",
                ["LimitesDeTaxa:Cadastro:PermitLimit"] = "1000",
                ["LimitesDeTaxa:ConsultaPublica:PermitLimit"] = "1000",
            });
        _ = _fabrica.Services;

        var sufixo = Guid.NewGuid().ToString("N");
        _adminAUsuario = $"admin.a.{sufixo}";
        _adminBUsuario = $"admin.b.{sufixo}";
        _operadorAUsuario = $"operador.a.{sufixo}";
        _adminSemVinculosUsuario = $"admin.c.{sufixo}";
        _adminSemVinculosEmail = $"admin.c.{sufixo}@example.org";
        _cidadaoEmail = $"cidadao.{sufixo}@example.org";
        _cidadaoNome = $"Cidadão {sufixo}";
        _cidadaoTelefone = "(62) 99999-0000";

        await using var escopo = _fabrica.Services.CreateAsyncScope();
        var registrarOrganizacao = escopo.ServiceProvider.GetRequiredService<RegistrarOrganizacaoHandler>();
        var registrarCidadao = escopo.ServiceProvider.GetRequiredService<RegistrarCidadaoHandler>();

        var organizacaoA = await registrarOrganizacao.ExecutarAsync(
            new RegistroDeOrganizacaoEntrada(
                NomeOrganizacao: $"Organização A {sufixo}",
                Tipo: TipoOrganizacao.Cooperativa,
                Usuario: _adminAUsuario,
                Email: $"{_adminAUsuario}@example.org",
                Senha: Senha),
            Guid.NewGuid());
        Assert.IsTrue(organizacaoA.IsSuccess, Descrever(organizacaoA.Errors));
        _organizacaoAId = organizacaoA.Value.OrganizacaoId;

        var organizacaoB = await registrarOrganizacao.ExecutarAsync(
            new RegistroDeOrganizacaoEntrada(
                NomeOrganizacao: $"Organização B {sufixo}",
                Tipo: TipoOrganizacao.Associacao,
                Usuario: _adminBUsuario,
                Email: $"{_adminBUsuario}@example.org",
                Senha: Senha),
            Guid.NewGuid());
        Assert.IsTrue(organizacaoB.IsSuccess, Descrever(organizacaoB.Errors));
        _organizacaoBId = organizacaoB.Value.OrganizacaoId;

        var organizacaoSemVinculos = await registrarOrganizacao.ExecutarAsync(
            new RegistroDeOrganizacaoEntrada(
                NomeOrganizacao: $"Organização C {sufixo}",
                Tipo: TipoOrganizacao.Prefeitura,
                Usuario: _adminSemVinculosUsuario,
                Email: _adminSemVinculosEmail,
                Senha: Senha),
            Guid.NewGuid());
        Assert.IsTrue(organizacaoSemVinculos.IsSuccess, Descrever(organizacaoSemVinculos.Errors));
        _organizacaoSemVinculosId = organizacaoSemVinculos.Value.OrganizacaoId;

        await CriarOperadorAsync(escopo.ServiceProvider, _organizacaoAId, _operadorAUsuario);

        var cidadao = await registrarCidadao.ExecutarAsync(
            new RegistroDeCidadaoEntrada(
                Nome: _cidadaoNome,
                Email: _cidadaoEmail,
                Telefone: _cidadaoTelefone,
                Senha: Senha),
            Guid.NewGuid());
        Assert.IsTrue(cidadao.IsSuccess, Descrever(cidadao.Errors));

        await using var contextoDeVinculos = CriarContexto();
        contextoDeVinculos.OrganizacaoMunicipios.AddRange(
            CriarVinculoDeMunicipio(_organizacaoAId, _municipioA),
            CriarVinculoDeMunicipio(_organizacaoBId, _municipioB));
        contextoDeVinculos.OrganizacaoTipos.AddRange(
            CriarVinculoDeTipo(_organizacaoAId, _tipoPapelId),
            CriarVinculoDeTipo(_organizacaoBId, _tipoMetalId));
        await contextoDeVinculos.SaveChangesAsync();
    }

    private static async Task CriarOperadorAsync(
        IServiceProvider servicos,
        Guid organizacaoId,
        string usuario)
    {
        var gerenciador = servicos.GetRequiredService<UserManager<Usuario>>();
        var conta = new Usuario
        {
            UserName = usuario,
            Email = $"{usuario}@example.org",
            Nome = $"Operador {usuario}",
            OrganizacaoId = organizacaoId,
            Ativo = true,
            TrocaSenhaPendente = false,
            CriadoEmUtc = Agora,
            AtualizadoEmUtc = Agora,
        };

        var criacao = await gerenciador.CreateAsync(conta, Senha);
        Assert.IsTrue(criacao.Succeeded, string.Join("; ", criacao.Errors.Select(erro => erro.Description)));

        var atribuicao = await gerenciador.AddToRoleAsync(conta, Papeis.Operador);
        Assert.IsTrue(atribuicao.Succeeded, string.Join("; ", atribuicao.Errors.Select(erro => erro.Description)));
    }

    private static OrganizacaoMunicipio CriarVinculoDeMunicipio(Guid organizacaoId, int municipioId)
    {
        var vinculo = OrganizacaoMunicipio.Criar(municipioId, Agora);
        ((IEntidadeDaOrganizacao)vinculo).OrganizacaoId = organizacaoId;
        return vinculo;
    }

    private static OrganizacaoTipo CriarVinculoDeTipo(Guid organizacaoId, Guid tipoResiduoId)
    {
        var vinculo = OrganizacaoTipo.Criar(tipoResiduoId, aceitaRetiradaDomiciliar: false, Agora);
        ((IEntidadeDaOrganizacao)vinculo).OrganizacaoId = organizacaoId;
        return vinculo;
    }

    private static string Descrever(IEnumerable<IError> erros)
    {
        return string.Join("; ", erros.Select(erro => erro.Message));
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
}
