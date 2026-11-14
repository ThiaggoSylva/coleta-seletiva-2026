using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Application;
using ColetaSeletiva.Application.Acesso.RegistroDeCidadao;
using ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;
using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Identidade;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ColetaSeletiva.IntegrationTests.Acesso;

[TestClass]
[DoNotParallelize]
public sealed class EquipeApiTests
{
    private const string NomeBanco = "ColetaSeletivaEquipeApiTests";
    private const string Senha = "SenhaForte!123";
    private const string SenhaOperador = "OperadorForte!456";

    private static ApiDeTeste _fabrica = null!;
    private static ServiceProvider _provedor = null!;
    private static Guid _organizacaoAId;
    private static Guid _organizacaoBId;
    private static Guid _adminAId;
    private static Guid _adminBId;
    private static string _adminAUsuario = null!;
    private static string _adminAEmail = null!;
    private static string _adminBUsuario = null!;
    private static string _adminBEmail = null!;
    private static string _cidadaoEmail = null!;

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
    public async Task Administrador_cria_operador_com_papel_fixo_e_troca_de_senha_pendente()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenAdmin = await EntrarAsync(cliente, _adminAUsuario);
        var correlacaoId = Guid.NewGuid();
        var sufixo = Guid.NewGuid().ToString("N");

        var requisicao = CriarRequisicaoAutenticada(HttpMethod.Post, "/api/v1/equipe", tokenAdmin);
        requisicao.Headers.Add("X-Correlation-Id", correlacaoId.ToString());
        requisicao.Content = JsonContent.Create(new
        {
            nome = $"Operador {sufixo}",
            usuario = $"operador.{sufixo}",
            email = $"operador.{sufixo}@example.org",
            senha = SenhaOperador,
            papel = "Administrador",
            organizacaoId = _organizacaoBId,
        });

        var resposta = await cliente.SendAsync(requisicao);

        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var id = corpo.RootElement.GetProperty("id").GetGuid();
        Assert.AreEqual(
            $"/api/v1/equipe/{id}",
            resposta.Headers.Location?.ToString());
        Assert.AreEqual(
            $"Operador {sufixo}",
            corpo.RootElement.GetProperty("nome").GetString());
        Assert.AreEqual(
            $"operador.{sufixo}",
            corpo.RootElement.GetProperty("usuario").GetString());
        Assert.AreEqual(
            $"operador.{sufixo}@example.org",
            corpo.RootElement.GetProperty("email").GetString());
        Assert.IsTrue(corpo.RootElement.GetProperty("ativo").GetBoolean());
        Assert.IsTrue(corpo.RootElement.GetProperty("trocaSenhaPendente").GetBoolean());

        var persistido = await ObterUsuarioAsync(id);
        Assert.AreEqual(_organizacaoAId, persistido.OrganizacaoId);
        Assert.IsTrue(persistido.Ativo);
        Assert.IsTrue(persistido.TrocaSenhaPendente);
        CollectionAssert.AreEqual(
            new[] { Papeis.Operador },
            (await ObterPapeisAsync(id)).ToArray());

        var auditoria = await ObterAuditoriaAsync(AcoesAuditadas.OperadorCriado, id);
        Assert.AreEqual(_organizacaoAId, auditoria.OrganizacaoId);
        Assert.IsNotNull(auditoria.AtorUsuarioId);
        Assert.AreEqual(EntidadesAuditadas.Usuario, auditoria.Entidade);
        Assert.AreEqual(correlacaoId, auditoria.CorrelacaoId);
        Assert.IsNotNull(auditoria.CamposAlterados);
        Assert.IsFalse(auditoria.CamposAlterados.Contains(SenhaOperador, StringComparison.Ordinal));
        Assert.IsFalse(auditoria.CamposAlterados.Contains($"operador.{sufixo}", StringComparison.Ordinal));

        var login = await cliente.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { identificador = $"operador.{sufixo}", senha = SenhaOperador });
        Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);
        using var corpoLogin = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var usuario = corpoLogin.RootElement.GetProperty("usuario");
        Assert.AreEqual(Papeis.Operador, usuario.GetProperty("papel").GetString());
        Assert.AreEqual(_organizacaoAId, usuario.GetProperty("organizacaoId").GetGuid());
        Assert.IsTrue(usuario.GetProperty("trocaSenhaPendente").GetBoolean());
    }

    [TestMethod]
    public async Task Duplicidade_global_rejeita_cadastro_sem_criar_conta()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenAdmin = await EntrarAsync(cliente, _adminAUsuario);
        var sufixo = Guid.NewGuid().ToString("N");
        var operador = await CriarOperadorAsync(
            cliente,
            tokenAdmin,
            sufixo,
            $"operador.{sufixo}",
            $"operador.{sufixo}@example.org");

        var totalAntes = await ContarUsuariosAsync();

        var usuarioDuplicado = await EnviarCriacaoAsync(
            cliente,
            tokenAdmin,
            $"Operador {sufixo} B",
            operador.Usuario,
            $"outro.{sufixo}@example.org",
            SenhaOperador);
        var emailDuplicado = await EnviarCriacaoAsync(
            cliente,
            tokenAdmin,
            $"Operador {sufixo} C",
            $"outro.{sufixo}",
            operador.Email,
            SenhaOperador);
        var usuarioDeOutraOrganizacao = await EnviarCriacaoAsync(
            cliente,
            tokenAdmin,
            $"Operador {sufixo} D",
            _adminBUsuario,
            $"outro.{sufixo}.d@example.org",
            SenhaOperador);
        var emailDeOutraOrganizacao = await EnviarCriacaoAsync(
            cliente,
            tokenAdmin,
            $"Operador {sufixo} E",
            $"outro.{sufixo}.e",
            _adminBEmail,
            SenhaOperador);

        Assert.AreEqual(HttpStatusCode.Conflict, usuarioDuplicado.StatusCode);
        Assert.AreEqual("usuario_em_uso", await LerCodigoAsync(usuarioDuplicado));
        Assert.AreEqual(HttpStatusCode.Conflict, emailDuplicado.StatusCode);
        Assert.AreEqual("email_em_uso", await LerCodigoAsync(emailDuplicado));
        Assert.AreEqual(HttpStatusCode.Conflict, usuarioDeOutraOrganizacao.StatusCode);
        Assert.AreEqual("usuario_em_uso", await LerCodigoAsync(usuarioDeOutraOrganizacao));
        Assert.AreEqual(HttpStatusCode.Conflict, emailDeOutraOrganizacao.StatusCode);
        Assert.AreEqual("email_em_uso", await LerCodigoAsync(emailDeOutraOrganizacao));

        Assert.AreEqual(totalAntes, await ContarUsuariosAsync());
    }

    [TestMethod]
    public async Task Edicao_altera_dados_e_ignora_papel_e_organizacao_enviados()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenAdmin = await EntrarAsync(cliente, _adminAUsuario);
        var sufixo = Guid.NewGuid().ToString("N");
        var operador = await CriarOperadorAsync(
            cliente,
            tokenAdmin,
            sufixo,
            $"operador.{sufixo}",
            $"operador.{sufixo}@example.org");

        var correlacaoId = Guid.NewGuid();
        var requisicao = CriarRequisicaoAutenticada(
            HttpMethod.Put,
            $"/api/v1/equipe/{operador.Id}",
            tokenAdmin);
        requisicao.Headers.Add("X-Correlation-Id", correlacaoId.ToString());
        requisicao.Content = JsonContent.Create(new
        {
            nome = $"Operador Editado {sufixo}",
            usuario = $"editado.{sufixo}",
            email = $"editado.{sufixo}@example.org",
            papel = "Administrador",
            organizacaoId = _organizacaoBId,
            ativo = false,
        });

        var resposta = await cliente.SendAsync(requisicao);

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.AreEqual(
            $"Operador Editado {sufixo}",
            corpo.RootElement.GetProperty("nome").GetString());
        Assert.AreEqual(
            $"editado.{sufixo}",
            corpo.RootElement.GetProperty("usuario").GetString());
        Assert.AreEqual(
            $"editado.{sufixo}@example.org",
            corpo.RootElement.GetProperty("email").GetString());
        Assert.IsTrue(corpo.RootElement.GetProperty("ativo").GetBoolean());
        Assert.IsTrue(corpo.RootElement.GetProperty("trocaSenhaPendente").GetBoolean());

        var persistido = await ObterUsuarioAsync(operador.Id);
        Assert.AreEqual(_organizacaoAId, persistido.OrganizacaoId);
        Assert.IsTrue(persistido.Ativo);
        CollectionAssert.AreEqual(
            new[] { Papeis.Operador },
            (await ObterPapeisAsync(operador.Id)).ToArray());

        var auditoria = await ObterAuditoriaAsync(AcoesAuditadas.OperadorEditado, operador.Id);
        Assert.AreEqual(correlacaoId, auditoria.CorrelacaoId);
        Assert.AreEqual(_organizacaoAId, auditoria.OrganizacaoId);

        var loginAntigo = await cliente.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { identificador = operador.Email, senha = SenhaOperador });
        Assert.AreEqual(HttpStatusCode.Unauthorized, loginAntigo.StatusCode);

        var loginNovo = await cliente.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { identificador = $"editado.{sufixo}@example.org", senha = SenhaOperador });
        Assert.AreEqual(HttpStatusCode.OK, loginNovo.StatusCode);
    }

    [TestMethod]
    public async Task Edicao_rejeita_usuario_e_email_em_uso_global()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenAdmin = await EntrarAsync(cliente, _adminAUsuario);
        var sufixo = Guid.NewGuid().ToString("N");
        var primeiro = await CriarOperadorAsync(
            cliente,
            tokenAdmin,
            $"{sufixo}.1",
            $"operador.{sufixo}.1",
            $"operador.{sufixo}.1@example.org");
        var segundo = await CriarOperadorAsync(
            cliente,
            tokenAdmin,
            $"{sufixo}.2",
            $"operador.{sufixo}.2",
            $"operador.{sufixo}.2@example.org");

        var usuarioEmUso = await EnviarEdicaoAsync(
            cliente,
            tokenAdmin,
            segundo.Id,
            "Operador Dois",
            primeiro.Usuario,
            segundo.Email);
        var emailEmUso = await EnviarEdicaoAsync(
            cliente,
            tokenAdmin,
            segundo.Id,
            "Operador Dois",
            segundo.Usuario,
            primeiro.Email);
        var usuarioDeOutraOrganizacao = await EnviarEdicaoAsync(
            cliente,
            tokenAdmin,
            segundo.Id,
            "Operador Dois",
            _adminBUsuario,
            segundo.Email);

        Assert.AreEqual(HttpStatusCode.Conflict, usuarioEmUso.StatusCode);
        Assert.AreEqual("usuario_em_uso", await LerCodigoAsync(usuarioEmUso));
        Assert.AreEqual(HttpStatusCode.Conflict, emailEmUso.StatusCode);
        Assert.AreEqual("email_em_uso", await LerCodigoAsync(emailEmUso));
        Assert.AreEqual(HttpStatusCode.Conflict, usuarioDeOutraOrganizacao.StatusCode);
        Assert.AreEqual("usuario_em_uso", await LerCodigoAsync(usuarioDeOutraOrganizacao));

        var persistido = await ObterUsuarioAsync(segundo.Id);
        Assert.AreEqual(segundo.Usuario, persistido.UserName);
        Assert.AreEqual(segundo.Email, persistido.Email);
        Assert.AreEqual(_organizacaoAId, persistido.OrganizacaoId);
    }

    [TestMethod]
    public async Task Validacao_rejeita_dados_invalidos_sem_criar_conta()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenAdmin = await EntrarAsync(cliente, _adminAUsuario);
        var sufixo = Guid.NewGuid().ToString("N");
        var totalAntes = await ContarUsuariosAsync();

        var invalido = await EnviarCriacaoAsync(
            cliente,
            tokenAdmin,
            nome: string.Empty,
            usuario: string.Empty,
            email: "email-invalido",
            senha: string.Empty);
        Assert.AreEqual(HttpStatusCode.BadRequest, invalido.StatusCode);
        var erros = await LerErrosDeValidacaoAsync(invalido);
        Assert.IsTrue(erros.ContainsKey("nome"));
        Assert.IsTrue(erros.ContainsKey("usuario"));
        Assert.IsTrue(erros.ContainsKey("email"));
        Assert.IsTrue(erros.ContainsKey("senha"));

        var senhaFraca = await EnviarCriacaoAsync(
            cliente,
            tokenAdmin,
            $"Operador {sufixo}",
            $"operador.{sufixo}",
            $"operador.{sufixo}@example.org",
            "curta");
        Assert.AreEqual(HttpStatusCode.BadRequest, senhaFraca.StatusCode);
        Assert.IsTrue((await LerErrosDeValidacaoAsync(senhaFraca)).ContainsKey("senha"));

        Assert.AreEqual(totalAntes, await ContarUsuariosAsync());
    }

    [TestMethod]
    public async Task Operador_de_outra_organizacao_nao_e_consultado_nem_editado()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenAdminA = await EntrarAsync(cliente, _adminAUsuario);
        var tokenAdminB = await EntrarAsync(cliente, _adminBUsuario);
        var sufixo = Guid.NewGuid().ToString("N");
        var operadorB = await CriarOperadorAsync(
            cliente,
            tokenAdminB,
            sufixo,
            $"operador.b.{sufixo}",
            $"operador.b.{sufixo}@example.org");
        var nomeOriginal = operadorB.Nome;

        var consulta = await cliente.SendAsync(
            CriarRequisicaoAutenticada(
                HttpMethod.Get,
                $"/api/v1/equipe/{operadorB.Id}",
                tokenAdminA));
        Assert.AreEqual(HttpStatusCode.NotFound, consulta.StatusCode);
        Assert.AreEqual("nao_encontrado", await LerCodigoAsync(consulta));

        var edicao = await EnviarEdicaoAsync(
            cliente,
            tokenAdminA,
            operadorB.Id,
            "Nome forjado",
            $"forjado.{sufixo}",
            $"forjado.{sufixo}@example.org");
        Assert.AreEqual(HttpStatusCode.NotFound, edicao.StatusCode);
        Assert.AreEqual("nao_encontrado", await LerCodigoAsync(edicao));

        var persistido = await ObterUsuarioAsync(operadorB.Id);
        Assert.AreEqual(nomeOriginal, persistido.Nome);
        Assert.AreEqual(_organizacaoBId, persistido.OrganizacaoId);
    }

    [TestMethod]
    public async Task Administrador_lista_somente_operadores_da_propria_organizacao()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenA = await EntrarAsync(cliente, _adminAUsuario);
        var tokenB = await EntrarAsync(cliente, _adminBUsuario);
        var sufixo = Guid.NewGuid().ToString("N");
        var operadorA1 = await CriarOperadorAsync(
            cliente,
            tokenA,
            $"{sufixo}.a1",
            $"operador.{sufixo}.a1",
            $"operador.{sufixo}.a1@example.org");
        var operadorA2 = await CriarOperadorAsync(
            cliente,
            tokenA,
            $"{sufixo}.a2",
            $"operador.{sufixo}.a2",
            $"operador.{sufixo}.a2@example.org");
        var operadorB = await CriarOperadorAsync(
            cliente,
            tokenB,
            $"{sufixo}.b1",
            $"operador.{sufixo}.b1",
            $"operador.{sufixo}.b1@example.org");

        var resposta = await cliente.SendAsync(
            CriarRequisicaoAutenticada(HttpMethod.Get, "/api/v1/equipe", tokenA));

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var ids = corpo.RootElement
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToList();

        Assert.IsTrue(ids.Contains(operadorA1.Id));
        Assert.IsTrue(ids.Contains(operadorA2.Id));
        Assert.IsFalse(ids.Contains(operadorB.Id));
        Assert.IsFalse(ids.Contains(_adminAId));
        Assert.IsFalse(ids.Contains(_adminBId));

        var consulta = await cliente.SendAsync(
            CriarRequisicaoAutenticada(
                HttpMethod.Get,
                $"/api/v1/equipe/{operadorA1.Id}",
                tokenA));
        Assert.AreEqual(HttpStatusCode.OK, consulta.StatusCode);
        using var corpoConsulta = JsonDocument.Parse(await consulta.Content.ReadAsStringAsync());
        Assert.AreEqual(operadorA1.Id, corpoConsulta.RootElement.GetProperty("id").GetGuid());
        Assert.AreEqual(_organizacaoAId, (await ObterUsuarioAsync(operadorA1.Id)).OrganizacaoId);
    }

    [TestMethod]
    public async Task Inativacao_revoga_sessoes_impede_novo_login_e_preserva_historico()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenAdmin = await EntrarAsync(cliente, _adminAUsuario);
        var sufixo = Guid.NewGuid().ToString("N");
        var operador = await CriarOperadorAsync(
            cliente,
            tokenAdmin,
            sufixo,
            $"operador.{sufixo}",
            $"operador.{sufixo}@example.org");
        var sessao = await EntrarCompletoAsync(cliente, operador.Usuario, SenhaOperador);

        var correlacaoId = Guid.NewGuid();
        var requisicao = CriarRequisicaoAutenticada(
            HttpMethod.Delete,
            $"/api/v1/equipe/{operador.Id}",
            tokenAdmin);
        requisicao.Headers.Add("X-Correlation-Id", correlacaoId.ToString());

        var inativacao = await cliente.SendAsync(requisicao);

        Assert.AreEqual(HttpStatusCode.NoContent, inativacao.StatusCode);

        var persistido = await ObterUsuarioAsync(operador.Id);
        Assert.IsFalse(persistido.Ativo);
        Assert.AreEqual(_organizacaoAId, persistido.OrganizacaoId);

        var renovacao = await EnviarRefreshAsync(cliente, sessao.Refresh);
        Assert.AreEqual(HttpStatusCode.Unauthorized, renovacao.StatusCode);

        var novoLogin = await cliente.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { identificador = operador.Usuario, senha = SenhaOperador });
        Assert.AreEqual(HttpStatusCode.Unauthorized, novoLogin.StatusCode);

        var contaAtual = await cliente.SendAsync(
            CriarRequisicaoAutenticada(HttpMethod.Get, "/api/v1/auth/me", sessao.Token));
        Assert.AreEqual(HttpStatusCode.Unauthorized, contaAtual.StatusCode);

        var tokens = await ObterRefreshTokensAsync(operador.Id);
        Assert.IsTrue(tokens.Count > 0);
        Assert.IsTrue(tokens.All(token => token.RevogadoEmUtc is not null));

        var auditoria = await ObterAuditoriaAsync(
            AcoesAuditadas.OperadorInativado,
            operador.Id);
        Assert.AreEqual(_organizacaoAId, auditoria.OrganizacaoId);
        Assert.AreEqual(_adminAId, auditoria.AtorUsuarioId);
        Assert.AreEqual(correlacaoId, auditoria.CorrelacaoId);
        Assert.AreEqual(EntidadesAuditadas.Usuario, auditoria.Entidade);
        Assert.AreEqual("ativo", auditoria.CamposAlterados);

        var auditoriasDoOperador = await ObterAuditoriasDoOperadorAsync(operador.Id);
        Assert.IsTrue(auditoriasDoOperador.Any());
        Assert.IsTrue(auditoriasDoOperador.Any(item => item.AtorUsuarioId == operador.Id));
    }

    [TestMethod]
    public async Task Inativacao_de_operador_de_outra_organizacao_retorna_404()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenA = await EntrarAsync(cliente, _adminAUsuario);
        var tokenB = await EntrarAsync(cliente, _adminBUsuario);
        var sufixo = Guid.NewGuid().ToString("N");
        var operadorB = await CriarOperadorAsync(
            cliente,
            tokenB,
            sufixo,
            $"operador.b.{sufixo}",
            $"operador.b.{sufixo}@example.org");

        var resposta = await cliente.SendAsync(
            CriarRequisicaoAutenticada(
                HttpMethod.Delete,
                $"/api/v1/equipe/{operadorB.Id}",
                tokenA));

        Assert.AreEqual(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.AreEqual("nao_encontrado", await LerCodigoAsync(resposta));
        Assert.IsTrue((await ObterUsuarioAsync(operadorB.Id)).Ativo);
    }

    [TestMethod]
    public async Task Operador_e_cidadao_recebem_403_em_todas_as_rotas_de_equipe()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenAdmin = await EntrarAsync(cliente, _adminAUsuario);
        var sufixo = Guid.NewGuid().ToString("N");
        var operador = await CriarOperadorAsync(
            cliente,
            tokenAdmin,
            sufixo,
            $"operador.{sufixo}",
            $"operador.{sufixo}@example.org");
        await TornarOperadorSemTrocaPendenteAsync(operador.Id);
        var tokenOperador = await EntrarAsync(cliente, operador.Usuario, SenhaOperador);
        var tokenCidadao = await EntrarAsync(cliente, _cidadaoEmail);
        var totalAntes = await ContarUsuariosAsync();
        var nomeOriginal = (await ObterUsuarioAsync(operador.Id)).Nome;

        var rotas = new (HttpMethod Metodo, Guid? OperadorId, bool EnviaCorpo)[]
        {
            (HttpMethod.Get, null, false),
            (HttpMethod.Post, null, true),
            (HttpMethod.Get, operador.Id, false),
            (HttpMethod.Put, operador.Id, true),
            (HttpMethod.Delete, operador.Id, false),
        };

        foreach (var token in new[] { tokenOperador, tokenCidadao })
        {
            foreach (var rota in rotas)
            {
                var resposta = await cliente.SendAsync(
                    CriarRequisicaoDaEquipe(
                        rota.Metodo,
                        token,
                        rota.OperadorId,
                        rota.EnviaCorpo ? CriarCorpoDeOperador(sufixo) : null));

                Assert.AreEqual(
                    HttpStatusCode.Forbidden,
                    resposta.StatusCode,
                    $"{rota.Metodo} {rota.OperadorId} deveria negar o papel.");
            }
        }

        foreach (var rota in rotas)
        {
            var resposta = await cliente.SendAsync(
                CriarRequisicaoDaEquipe(
                    rota.Metodo,
                    token: null,
                    rota.OperadorId,
                    rota.EnviaCorpo ? CriarCorpoDeOperador(sufixo) : null));

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                resposta.StatusCode,
                $"{rota.Metodo} {rota.OperadorId} deveria exigir autenticação.");
        }

        Assert.AreEqual(totalAntes, await ContarUsuariosAsync());
        Assert.AreEqual(nomeOriginal, (await ObterUsuarioAsync(operador.Id)).Nome);
        Assert.IsTrue((await ObterUsuarioAsync(operador.Id)).Ativo);
    }

    private static HttpRequestMessage CriarRequisicaoDaEquipe(
        HttpMethod metodo,
        string? token,
        Guid? operadorId = null,
        HttpContent? conteudo = null)
    {
        var caminho = operadorId is null
            ? "/api/v1/equipe"
            : $"/api/v1/equipe/{operadorId}";
        var requisicao = new HttpRequestMessage(metodo, caminho);

        if (token is not null)
        {
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (conteudo is not null)
        {
            requisicao.Content = conteudo;
        }

        return requisicao;
    }

    private static HttpContent CriarCorpoDeOperador(string sufixo)
    {
        return JsonContent.Create(new
        {
            nome = $"Operador Forjado {sufixo}",
            usuario = $"forjado.{sufixo}",
            email = $"forjado.{sufixo}@example.org",
            senha = SenhaOperador,
        });
    }

    private static async Task TornarOperadorSemTrocaPendenteAsync(Guid operadorId)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var gerenciador = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var operador = await gerenciador.FindByIdAsync(operadorId.ToString());
        Assert.IsNotNull(operador);
        operador.TrocaSenhaPendente = false;

        var resultado = await gerenciador.UpdateAsync(operador);
        Assert.IsTrue(
            resultado.Succeeded,
            string.Join("; ", resultado.Errors.Select(erro => $"{erro.Code}: {erro.Description}")));
    }

    private static async Task<OperadorCriado> CriarOperadorAsync(
        HttpClient cliente,
        string token,
        string sufixo,
        string usuario,
        string email)
    {
        var resposta = await EnviarCriacaoAsync(
            cliente,
            token,
            $"Operador {sufixo}",
            usuario,
            email,
            SenhaOperador);
        Assert.AreEqual(
            HttpStatusCode.Created,
            resposta.StatusCode,
            await resposta.Content.ReadAsStringAsync());

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        return new OperadorCriado(
            corpo.RootElement.GetProperty("id").GetGuid(),
            corpo.RootElement.GetProperty("nome").GetString()!,
            corpo.RootElement.GetProperty("usuario").GetString()!,
            corpo.RootElement.GetProperty("email").GetString()!);
    }

    private static async Task<HttpResponseMessage> EnviarCriacaoAsync(
        HttpClient cliente,
        string token,
        string nome,
        string usuario,
        string email,
        string senha)
    {
        var requisicao = CriarRequisicaoAutenticada(HttpMethod.Post, "/api/v1/equipe", token);
        requisicao.Content = JsonContent.Create(new { nome, usuario, email, senha });

        return await cliente.SendAsync(requisicao);
    }

    private static async Task<HttpResponseMessage> EnviarEdicaoAsync(
        HttpClient cliente,
        string token,
        Guid operadorId,
        string nome,
        string usuario,
        string email)
    {
        var requisicao = CriarRequisicaoAutenticada(
            HttpMethod.Put,
            $"/api/v1/equipe/{operadorId}",
            token);
        requisicao.Content = JsonContent.Create(new { nome, usuario, email });

        return await cliente.SendAsync(requisicao);
    }

    private static HttpRequestMessage CriarRequisicaoAutenticada(
        HttpMethod metodo,
        string caminho,
        string token)
    {
        var requisicao = new HttpRequestMessage(metodo, caminho);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return requisicao;
    }

    private static async Task<string> EntrarAsync(
        HttpClient cliente,
        string identificador,
        string senha = Senha)
    {
        var sessao = await EntrarCompletoAsync(cliente, identificador, senha);

        return sessao.Token;
    }

    private static async Task<SessaoDeLogin> EntrarCompletoAsync(
        HttpClient cliente,
        string identificador,
        string senha = Senha)
    {
        var resposta = await cliente.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { identificador, senha });
        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var token = corpo.RootElement.GetProperty("tokenDeAcesso").GetString()!;
        var refresh = ExtrairRefresh(ObterCookieDeRefresh(resposta));
        Assert.IsNotNull(refresh);

        return new SessaoDeLogin(token, refresh!);
    }

    private static async Task<HttpResponseMessage> EnviarRefreshAsync(
        HttpClient cliente,
        string refresh)
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        requisicao.Headers.TryAddWithoutValidation(
            "Cookie",
            $"{ConstantesDeSessao.NomeDoCookie}={refresh}");
        requisicao.Headers.TryAddWithoutValidation(
            ConstantesDeSessao.CabecalhoDeOrigem,
            "http://localhost");
        requisicao.Headers.TryAddWithoutValidation(
            ConstantesDeSessao.CabecalhoSolicitadoCom,
            ConstantesDeSessao.ValorSolicitadoCom);

        return await cliente.SendAsync(requisicao);
    }

    private static string? ObterCookieDeRefresh(HttpResponseMessage resposta)
    {
        if (!resposta.Headers.TryGetValues("Set-Cookie", out var valores))
        {
            return null;
        }

        return valores.FirstOrDefault(valor => valor.StartsWith(
            $"{ConstantesDeSessao.NomeDoCookie}=",
            StringComparison.Ordinal));
    }

    private static string? ExtrairRefresh(string? cookie)
    {
        if (cookie is null)
        {
            return null;
        }

        var valor = cookie.Split(';', 2)[0];
        var separador = valor.IndexOf('=', StringComparison.Ordinal);
        if (separador < 0)
        {
            return null;
        }

        var conteudo = valor[(separador + 1)..];
        return string.IsNullOrWhiteSpace(conteudo) ? null : conteudo;
    }

    private static async Task<string?> LerCodigoAsync(HttpResponseMessage resposta)
    {
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        return corpo.RootElement.TryGetProperty("code", out var codigo)
            ? codigo.GetString()
            : null;
    }

    private static async Task<Dictionary<string, string[]>> LerErrosDeValidacaoAsync(
        HttpResponseMessage resposta)
    {
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var erros = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var propriedade in corpo.RootElement.GetProperty("errors").EnumerateObject())
        {
            erros[propriedade.Name] = propriedade.Value
                .EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .ToArray();
        }

        return erros;
    }

    private static async Task<Usuario> ObterUsuarioAsync(Guid usuarioId)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();

        return await contexto.Usuarios
            .AsNoTracking()
            .SingleAsync(usuario => usuario.Id == usuarioId);
    }

    private static async Task<IList<string>> ObterPapeisAsync(Guid usuarioId)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var gerenciador = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var usuario = await gerenciador.FindByIdAsync(usuarioId.ToString());
        Assert.IsNotNull(usuario);

        return await gerenciador.GetRolesAsync(usuario);
    }

    private static async Task<int> ContarUsuariosAsync()
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();

        return await contexto.Usuarios.CountAsync();
    }

    private static async Task<Auditoria> ObterAuditoriaAsync(string acao, Guid entidadeId)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();

        return await contexto.Auditorias
            .AsNoTracking()
            .SingleAsync(auditoria =>
                auditoria.Acao == acao && auditoria.EntidadeId == entidadeId);
    }

    private static async Task<List<Auditoria>> ObterAuditoriasDoOperadorAsync(Guid operadorId)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();

        return await contexto.Auditorias
            .AsNoTracking()
            .Where(auditoria => auditoria.EntidadeId == operadorId)
            .ToListAsync();
    }

    private static async Task<List<RefreshToken>> ObterRefreshTokensAsync(Guid usuarioId)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();

        return await contexto.RefreshTokens
            .AsNoTracking()
            .Where(token => token.UsuarioId == usuarioId)
            .ToListAsync();
    }

    private static async Task InicializarAsync()
    {
        var connectionString = await SqlServerTestDatabase.ObterConnectionStringAsync(NomeBanco);

        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddInfrastructure(connectionString);
        _provedor = servicos.BuildServiceProvider();

        await using (var escopo = _provedor.CreateAsyncScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
            await contexto.Database.EnsureDeletedAsync();
            await contexto.Database.MigrateAsync();
        }

        _fabrica = new ApiDeTeste(
            connectionString,
            new Dictionary<string, string?>
            {
                ["LimitesDeTaxa:Login:PermitLimit"] = "1000",
                ["LimitesDeTaxa:Refresh:PermitLimit"] = "1000",
                ["LimitesDeTaxa:Cadastro:PermitLimit"] = "1000",
            });
        _ = _fabrica.Services;

        await using var escopoSemeando = _fabrica.Services.CreateAsyncScope();
        var registrarOrganizacao = escopoSemeando.ServiceProvider
            .GetRequiredService<RegistrarOrganizacaoHandler>();
        var registrarCidadao = escopoSemeando.ServiceProvider
            .GetRequiredService<RegistrarCidadaoHandler>();

        var sufixo = Guid.NewGuid().ToString("N");
        _adminAUsuario = $"admin.a.{sufixo}";
        _adminAEmail = $"admin.a.{sufixo}@example.org";
        _adminBUsuario = $"admin.b.{sufixo}";
        _adminBEmail = $"admin.b.{sufixo}@example.org";
        _cidadaoEmail = $"cidadao.{sufixo}@example.org";

        var organizacaoA = await registrarOrganizacao.ExecutarAsync(
            new RegistroDeOrganizacaoEntrada(
                NomeOrganizacao: $"Organização A {sufixo}",
                Tipo: TipoOrganizacao.Cooperativa,
                Usuario: _adminAUsuario,
                Email: _adminAEmail,
                Senha: Senha),
            Guid.NewGuid());
        Assert.IsTrue(
            organizacaoA.IsSuccess,
            string.Join("; ", organizacaoA.Errors.Select(erro => erro.Message)));
        _organizacaoAId = organizacaoA.Value.OrganizacaoId;
        _adminAId = organizacaoA.Value.AdministradorId;

        var organizacaoB = await registrarOrganizacao.ExecutarAsync(
            new RegistroDeOrganizacaoEntrada(
                NomeOrganizacao: $"Organização B {sufixo}",
                Tipo: TipoOrganizacao.Associacao,
                Usuario: _adminBUsuario,
                Email: _adminBEmail,
                Senha: Senha),
            Guid.NewGuid());
        Assert.IsTrue(
            organizacaoB.IsSuccess,
            string.Join("; ", organizacaoB.Errors.Select(erro => erro.Message)));
        _organizacaoBId = organizacaoB.Value.OrganizacaoId;
        _adminBId = organizacaoB.Value.AdministradorId;

        var cidadao = await registrarCidadao.ExecutarAsync(
            new RegistroDeCidadaoEntrada(
                Nome: $"Cidadão {sufixo}",
                Email: _cidadaoEmail,
                Telefone: "(11) 99999-0000",
                Senha: Senha),
            Guid.NewGuid());
        Assert.IsTrue(
            cidadao.IsSuccess,
            string.Join("; ", cidadao.Errors.Select(erro => erro.Message)));
    }

    private static async Task LimparAsync()
    {
        if (_fabrica is not null)
        {
            await _fabrica.DisposeAsync();
        }

        if (_provedor is not null)
        {
            await using (var escopo = _provedor.CreateAsyncScope())
            {
                var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
                await contexto.Database.EnsureDeletedAsync();
            }

            await _provedor.DisposeAsync();
        }
    }

    private sealed record OperadorCriado(
        Guid Id,
        string Nome,
        string Usuario,
        string Email);

    private sealed record SessaoDeLogin(
        string Token,
        string Refresh);
}
