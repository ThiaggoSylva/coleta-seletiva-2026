using System.Text;
using ColetaSeletiva.Application;
using ColetaSeletiva.Application.Acesso.RegistroDeCidadao;
using ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;
using ColetaSeletiva.Application.Acesso.Sessao;
using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Identidade;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.Infrastructure.Seguranca;
using ColetaSeletiva.Infrastructure.Tenancy;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ColetaSeletiva.IntegrationTests.Acesso;

[TestClass]
[DoNotParallelize]
public sealed class SessaoTests
{
    private const string NomeBanco = "ColetaSeletivaSessaoTests";
    private const string SenhaValida = "SenhaForte!123";
    private const string NovaSenhaValida = "NovaSenhaForte!456";
    private const string Emissor = "coleta-seletiva-testes";
    private const string Publico = "coleta-seletiva-testes-web";
    private const string Chave = "chave-de-testes-com-pelo-menos-32-bytes!!";

    private static readonly Guid CorrelacaoId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private static ServiceProvider _provedor = null!;

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
    public async Task Administrador_entra_por_usuario_ou_email_com_token_de_15_minutos()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var organizacao = await RegistrarOrganizacaoAsync(sufixo);

        var porUsuario = await IniciarAsync($"admin.{sufixo}");
        var porEmail = await IniciarAsync($"admin.{sufixo}@example.org");

        Assert.IsTrue(porUsuario.IsSuccess, Descrever(porUsuario));
        Assert.IsTrue(porEmail.IsSuccess, Descrever(porEmail));

        var sessao = porUsuario.Value;
        Assert.AreEqual(Papeis.Administrador, sessao.Conta.Papel);
        Assert.AreEqual(organizacao.OrganizacaoId, sessao.Conta.OrganizacaoId);
        Assert.AreEqual($"admin.{sufixo}@example.org", sessao.Conta.Email);

        var jwt = await LerTokenValidadoAsync(sessao.TokenDeAcesso);
        Assert.AreEqual(sessao.Conta.Id.ToString(), jwt.Subject);
        Assert.AreEqual(Papeis.Administrador, jwt.GetClaim("role").Value);
        Assert.AreEqual(organizacao.OrganizacaoId.ToString(), jwt.GetClaim("organizacao_id").Value);
        Assert.IsFalse(string.IsNullOrWhiteSpace(jwt.GetClaim("jti").Value));
        Assert.IsTrue(jwt.ValidTo > DateTime.UtcNow.AddMinutes(14));
        Assert.IsTrue(jwt.ValidTo <= DateTime.UtcNow.AddMinutes(16));

        var horasParaExpirar = sessao.ExpiraEmUtc - DateTimeOffset.UtcNow;
        Assert.IsTrue(horasParaExpirar > TimeSpan.FromMinutes(14));
        Assert.IsTrue(horasParaExpirar <= TimeSpan.FromMinutes(16));

        var refresh = await ObterRefreshAsync(sessao.RefreshToken);
        Assert.AreEqual(sessao.Conta.Id, refresh.UsuarioId);
        Assert.AreNotEqual(sessao.RefreshToken, refresh.Hash);
        Assert.AreEqual(RefreshTokenOpaco.CalcularHash(sessao.RefreshToken), refresh.Hash);
        Assert.IsNull(refresh.ConsumidoEmUtc);
        Assert.IsNull(refresh.RevogadoEmUtc);

        var diasParaExpirar = refresh.ExpiraEmUtc - DateTimeOffset.UtcNow;
        Assert.IsTrue(diasParaExpirar > TimeSpan.FromDays(6.9));
        Assert.IsTrue(diasParaExpirar <= TimeSpan.FromDays(7));
    }

    [TestMethod]
    public async Task Cidadao_entra_somente_por_email_e_token_nao_tem_organizacao()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        await RegistrarCidadaoAsync(sufixo);

        var porEmail = await IniciarAsync($"cidadao.{sufixo}@example.org");
        Assert.IsTrue(porEmail.IsSuccess, Descrever(porEmail));
        Assert.AreEqual(Papeis.Cidadao, porEmail.Value.Conta.Papel);
        Assert.IsNull(porEmail.Value.Conta.OrganizacaoId);

        var jwt = await LerTokenValidadoAsync(porEmail.Value.TokenDeAcesso);
        Assert.AreEqual(Papeis.Cidadao, jwt.GetClaim("role").Value);
        Assert.IsFalse(jwt.TryGetClaim("organizacao_id", out _));

        var usuario = await CriarUsuarioAsync(
            usuario: $"cidadao.usuario.{sufixo}",
            email: $"cidadao.outro.{sufixo}@example.org",
            organizacaoId: null,
            papel: Papeis.Cidadao);

        var porUsuario = await IniciarAsync(usuario.UserName!);
        Assert.IsTrue(porUsuario.IsFailed);
        Assert.AreEqual(
            MensagensDeSessao.CredenciaisInvalidas,
            porUsuario.Errors.OfType<ErroNaoAutenticado>().Single().Message);

        var porEmailDoUsuario = await IniciarAsync(usuario.Email!);
        Assert.IsTrue(porEmailDoUsuario.IsSuccess, Descrever(porEmailDoUsuario));
        Assert.AreEqual(usuario.Id, porEmailDoUsuario.Value.Conta.Id);
    }

    [TestMethod]
    public async Task Credenciais_invalidas_e_conta_inativa_usam_a_mesma_mensagem_generica()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var organizacao = await RegistrarOrganizacaoAsync(sufixo);

        var operadorInativo = await CriarUsuarioAsync(
            usuario: $"operador.inativo.{sufixo}",
            email: $"operador.inativo.{sufixo}@example.org",
            organizacaoId: organizacao.OrganizacaoId,
            papel: Papeis.Operador,
            ativo: false);

        var senhaIncorreta = await IniciarAsync($"admin.{sufixo}", "SenhaErrada!999");
        var inexistente = await IniciarAsync($"nao.existe.{sufixo}", SenhaValida);
        var inativo = await IniciarAsync(operadorInativo.UserName!, SenhaValida);

        Assert.IsTrue(senhaIncorreta.IsFailed);
        Assert.IsTrue(inexistente.IsFailed);
        Assert.IsTrue(inativo.IsFailed);

        var mensagens = new[] { senhaIncorreta, inexistente, inativo }
            .Select(resultado => resultado.Errors.OfType<ErroNaoAutenticado>().Single().Message)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        CollectionAssert.AreEqual(
            new List<string> { MensagensDeSessao.CredenciaisInvalidas },
            mensagens);

        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
        Assert.IsFalse(
            await contexto.RefreshTokens.AnyAsync(token => token.UsuarioId == operadorInativo.Id));
    }

    [TestMethod]
    public async Task Refresh_rotaciona_o_token_e_reuso_revoga_a_familia()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        await RegistrarOrganizacaoAsync(sufixo);
        var login = await IniciarAsync($"admin.{sufixo}");
        Assert.IsTrue(login.IsSuccess, Descrever(login));

        var refreshOriginal = await ObterRefreshAsync(login.Value.RefreshToken);
        var familiaId = refreshOriginal.FamiliaId;
        var idOriginal = refreshOriginal.Id;
        var renovacao = await RenovarAsync(login.Value.RefreshToken);

        Assert.IsTrue(renovacao.IsSuccess, Descrever(renovacao));
        Assert.AreNotEqual(login.Value.RefreshToken, renovacao.Value.RefreshToken);
        Assert.AreEqual(login.Value.Conta.Id, renovacao.Value.Conta.Id);

        var refreshOriginalAtualizado = await ObterRefreshAsync(login.Value.RefreshToken);
        var refreshRenovado = await ObterRefreshAsync(renovacao.Value.RefreshToken);
        Assert.AreEqual(familiaId, refreshRenovado.FamiliaId);
        Assert.AreEqual(idOriginal, refreshOriginalAtualizado.Id);
        Assert.AreNotEqual(refreshOriginalAtualizado.Id, refreshRenovado.Id);
        Assert.IsNotNull(refreshOriginalAtualizado.ConsumidoEmUtc);
        Assert.IsNull(refreshOriginalAtualizado.RevogadoEmUtc);
        Assert.IsNull(refreshRenovado.ConsumidoEmUtc);
        Assert.IsNull(refreshRenovado.RevogadoEmUtc);

        var reuso = await RenovarAsync(login.Value.RefreshToken);

        Assert.IsTrue(reuso.IsFailed);
        Assert.AreEqual(
            MensagensDeSessao.SessaoInvalida,
            reuso.Errors.OfType<ErroNaoAutenticado>().Single().Message);

        var tokensDaFamilia = await ObterFamiliaAsync(familiaId);
        Assert.AreEqual(2, tokensDaFamilia.Count);
        Assert.IsTrue(tokensDaFamilia.All(token => token.RevogadoEmUtc is not null));

        var renovacaoAposReuso = await RenovarAsync(renovacao.Value.RefreshToken);
        Assert.IsTrue(renovacaoAposReuso.IsFailed);
    }

    [TestMethod]
    public async Task Logout_revoga_a_familia_registra_auditoria_e_e_idempotente()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        await RegistrarOrganizacaoAsync(sufixo);
        var login = await IniciarAsync($"admin.{sufixo}");
        Assert.IsTrue(login.IsSuccess, Descrever(login));

        var refresh = await ObterRefreshAsync(login.Value.RefreshToken);

        var logout = await EncerrarAsync(login.Value.RefreshToken);
        Assert.IsTrue(logout.IsSuccess);

        var tokensDaFamilia = await ObterFamiliaAsync(refresh.FamiliaId);
        Assert.IsTrue(tokensDaFamilia.All(token => token.RevogadoEmUtc is not null));

        var renovacao = await RenovarAsync(login.Value.RefreshToken);
        Assert.IsTrue(renovacao.IsFailed);

        var segundoLogout = await EncerrarAsync(login.Value.RefreshToken);
        Assert.IsTrue(segundoLogout.IsSuccess);

        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
        var auditorias = await contexto.Auditorias
            .AsNoTracking()
            .Where(auditoria => auditoria.EntidadeId == login.Value.Conta.Id
                && auditoria.Acao == AcoesAuditadas.SessaoEncerrada)
            .ToListAsync();

        Assert.AreEqual(1, auditorias.Count);
        Assert.AreEqual(login.Value.Conta.OrganizacaoId, auditorias[0].OrganizacaoId);
        Assert.AreEqual(login.Value.Conta.Id, auditorias[0].AtorUsuarioId);
        Assert.AreEqual(EntidadesAuditadas.Usuario, auditorias[0].Entidade);
        Assert.AreEqual(CorrelacaoId, auditorias[0].CorrelacaoId);
    }

    [TestMethod]
    public async Task Refresh_expirado_e_recusado_sem_renovar()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        await RegistrarOrganizacaoAsync(sufixo);
        var login = await IniciarAsync($"admin.{sufixo}");
        Assert.IsTrue(login.IsSuccess, Descrever(login));

        await using (var escopo = _provedor.CreateAsyncScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
            var hash = RefreshTokenOpaco.CalcularHash(login.Value.RefreshToken);
            await contexto.RefreshTokens
                .Where(token => token.Hash == hash)
                .ExecuteUpdateAsync(atualizacao => atualizacao.SetProperty(
                    token => token.ExpiraEmUtc,
                    DateTimeOffset.UtcNow.AddSeconds(-1)));
        }

        var renovacao = await RenovarAsync(login.Value.RefreshToken);

        Assert.IsTrue(renovacao.IsFailed);
        Assert.IsInstanceOfType<ErroNaoAutenticado>(renovacao.Errors.Single());
    }

    [TestMethod]
    public async Task Troca_de_senha_pendente_revoga_todas_as_sessoes_e_exige_novo_login()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var organizacao = await RegistrarOrganizacaoAsync(sufixo);
        var operador = await CriarUsuarioAsync(
            usuario: $"operador.{sufixo}",
            email: $"operador.{sufixo}@example.org",
            organizacaoId: organizacao.OrganizacaoId,
            papel: Papeis.Operador,
            trocaSenhaPendente: true);

        var primeiraSessao = await IniciarAsync(operador.UserName!);
        var segundaSessao = await IniciarAsync(operador.Email!);
        Assert.IsTrue(primeiraSessao.IsSuccess, Descrever(primeiraSessao));
        Assert.IsTrue(segundaSessao.IsSuccess, Descrever(segundaSessao));
        Assert.IsTrue(primeiraSessao.Value.Conta.TrocaSenhaPendente);

        var jwtPendente = await LerTokenValidadoAsync(primeiraSessao.Value.TokenDeAcesso);
        Assert.AreEqual(
            "true",
            jwtPendente.GetClaim(ClaimsColetaSeletiva.TrocaSenhaPendente).Value);

        var refreshPrimeira = await ObterRefreshAsync(primeiraSessao.Value.RefreshToken);
        var refreshSegunda = await ObterRefreshAsync(segundaSessao.Value.RefreshToken);
        Assert.AreNotEqual(refreshPrimeira.FamiliaId, refreshSegunda.FamiliaId);

        var alteracao = await AlterarSenhaAsync(
            operador.Id,
            organizacao.OrganizacaoId,
            ehCidadao: false,
            new AlterarSenhaEntrada(SenhaValida, NovaSenhaValida));

        Assert.IsTrue(alteracao.IsSuccess, Descrever(alteracao));

        var usuarioAtualizado = await ObterUsuarioAsync(operador.Id);
        Assert.IsFalse(usuarioAtualizado.TrocaSenhaPendente);
        await using (var escopo = _provedor.CreateAsyncScope())
        {
            var gerenciador = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            Assert.IsTrue(
                await gerenciador.CheckPasswordAsync(usuarioAtualizado, NovaSenhaValida));
        }

        var tokensDoUsuario = await ObterTokensDoUsuarioAsync(operador.Id);
        Assert.AreEqual(2, tokensDoUsuario.Count);
        Assert.IsTrue(tokensDoUsuario.All(token => token.RevogadoEmUtc is not null));

        var renovacao = await RenovarAsync(primeiraSessao.Value.RefreshToken);
        Assert.IsTrue(renovacao.IsFailed);

        var loginComSenhaAntiga = await IniciarAsync(operador.UserName!);
        Assert.IsTrue(loginComSenhaAntiga.IsFailed);
        Assert.AreEqual(
            MensagensDeSessao.CredenciaisInvalidas,
            loginComSenhaAntiga.Errors.OfType<ErroNaoAutenticado>().Single().Message);

        var loginComSenhaNova = await IniciarAsync(operador.UserName!, NovaSenhaValida);
        Assert.IsTrue(loginComSenhaNova.IsSuccess, Descrever(loginComSenhaNova));
        Assert.IsFalse(loginComSenhaNova.Value.Conta.TrocaSenhaPendente);

        var jwtNovo = await LerTokenValidadoAsync(loginComSenhaNova.Value.TokenDeAcesso);
        Assert.IsFalse(
            jwtNovo.TryGetClaim(ClaimsColetaSeletiva.TrocaSenhaPendente, out _));

        await using var escopoAuditoria = _provedor.CreateAsyncScope();
        var contexto = escopoAuditoria.ServiceProvider
            .GetRequiredService<ColetaSeletivaDbContext>();
        var auditorias = await contexto.Auditorias
            .AsNoTracking()
            .Where(auditoria => auditoria.EntidadeId == operador.Id
                && auditoria.Acao == AcoesAuditadas.SenhaAlterada)
            .ToListAsync();

        Assert.AreEqual(1, auditorias.Count);
        Assert.AreEqual(organizacao.OrganizacaoId, auditorias[0].OrganizacaoId);
        Assert.AreEqual(operador.Id, auditorias[0].AtorUsuarioId);
        Assert.AreEqual(EntidadesAuditadas.Usuario, auditorias[0].Entidade);
        Assert.AreEqual(CorrelacaoId, auditorias[0].CorrelacaoId);
    }

    [TestMethod]
    public async Task Troca_de_senha_rejeita_senha_atual_incorreta_e_nova_senha_fora_da_politica()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var organizacao = await RegistrarOrganizacaoAsync(sufixo);
        var operador = await CriarUsuarioAsync(
            usuario: $"operador.{sufixo}",
            email: $"operador.{sufixo}@example.org",
            organizacaoId: organizacao.OrganizacaoId,
            papel: Papeis.Operador,
            trocaSenhaPendente: true);

        var sessao = await IniciarAsync(operador.UserName!);
        Assert.IsTrue(sessao.IsSuccess, Descrever(sessao));

        var senhaIncorreta = await AlterarSenhaAsync(
            operador.Id,
            organizacao.OrganizacaoId,
            ehCidadao: false,
            new AlterarSenhaEntrada("SenhaErrada!999", NovaSenhaValida));

        Assert.IsTrue(senhaIncorreta.IsFailed);
        var erroSenhaAtual = senhaIncorreta.Errors.OfType<ErroDeValidacao>().Single();
        Assert.AreEqual(nameof(AlterarSenhaEntrada.SenhaAtual), erroSenhaAtual.Campo);
        Assert.AreEqual("A senha atual informada está incorreta.", erroSenhaAtual.Message);

        var senhaFraca = await AlterarSenhaAsync(
            operador.Id,
            organizacao.OrganizacaoId,
            ehCidadao: false,
            new AlterarSenhaEntrada(SenhaValida, "curta"));

        Assert.IsTrue(senhaFraca.IsFailed);
        var errosPolitica = senhaFraca.Errors.OfType<ErroDeValidacao>().ToList();
        Assert.IsTrue(errosPolitica.Count > 0);
        Assert.IsTrue(errosPolitica.All(erro => erro.Campo == "NovaSenha"));
        Assert.IsTrue(errosPolitica.Any(erro => erro.Message.Contains("12", StringComparison.Ordinal)));

        var usuario = await ObterUsuarioAsync(operador.Id);
        Assert.IsTrue(usuario.TrocaSenhaPendente);

        var loginComSenhaOriginal = await IniciarAsync(operador.UserName!);
        Assert.IsTrue(loginComSenhaOriginal.IsSuccess, Descrever(loginComSenhaOriginal));

        var refresh = await ObterRefreshAsync(sessao.Value.RefreshToken);
        Assert.IsNull(refresh.RevogadoEmUtc);

        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
        Assert.IsFalse(
            await contexto.Auditorias.AnyAsync(auditoria =>
                auditoria.EntidadeId == operador.Id
                && auditoria.Acao == AcoesAuditadas.SenhaAlterada));
    }

    private static async Task<Result> AlterarSenhaAsync(
        Guid usuarioId,
        Guid? organizacaoId,
        bool ehCidadao,
        AlterarSenhaEntrada entrada)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var tenantContext = escopo.ServiceProvider.GetRequiredService<TenantContext>();
        tenantContext.DefinirEscopo(usuarioId, organizacaoId, ehCidadao);

        var handler = escopo.ServiceProvider.GetRequiredService<AlterarSenhaHandler>();
        return await handler.ExecutarAsync(entrada, CorrelacaoId);
    }

    private static async Task<Usuario> ObterUsuarioAsync(Guid usuarioId)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var gerenciador = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        return await gerenciador.FindByIdAsync(usuarioId.ToString())
            ?? throw new InvalidOperationException("Usuário de teste não encontrado.");
    }

    private static async Task<List<RefreshToken>> ObterTokensDoUsuarioAsync(Guid usuarioId)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();

        return await contexto.RefreshTokens
            .AsNoTracking()
            .Where(token => token.UsuarioId == usuarioId)
            .ToListAsync();
    }

    private static async Task<ResultadoDeOrganizacao> RegistrarOrganizacaoAsync(string sufixo)
    {
        var entrada = new RegistroDeOrganizacaoEntrada(
            NomeOrganizacao: $"Organização {sufixo}",
            Tipo: TipoOrganizacao.Cooperativa,
            Usuario: $"admin.{sufixo}",
            Email: $"admin.{sufixo}@example.org",
            Senha: SenhaValida);

        await using var escopo = _provedor.CreateAsyncScope();
        var handler = escopo.ServiceProvider.GetRequiredService<RegistrarOrganizacaoHandler>();
        var resultado = await handler.ExecutarAsync(entrada, CorrelacaoId);

        Assert.IsTrue(resultado.IsSuccess, Descrever(resultado));

        return new ResultadoDeOrganizacao(
            resultado.Value.OrganizacaoId,
            resultado.Value.AdministradorId);
    }

    private static async Task<Guid> RegistrarCidadaoAsync(string sufixo)
    {
        var entrada = new RegistroDeCidadaoEntrada(
            Nome: $"Cidadão {sufixo}",
            Email: $"cidadao.{sufixo}@example.org",
            Telefone: "(11) 99999-0000",
            Senha: SenhaValida);

        await using var escopo = _provedor.CreateAsyncScope();
        var handler = escopo.ServiceProvider.GetRequiredService<RegistrarCidadaoHandler>();
        var resultado = await handler.ExecutarAsync(entrada, CorrelacaoId);

        Assert.IsTrue(resultado.IsSuccess, Descrever(resultado));

        return resultado.Value.UsuarioId;
    }

    private static async Task<Usuario> CriarUsuarioAsync(
        string usuario,
        string email,
        Guid? organizacaoId,
        string papel,
        bool ativo = true,
        bool trocaSenhaPendente = false)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var gerenciador = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var agora = DateTimeOffset.UtcNow;

        var conta = new Usuario
        {
            UserName = usuario,
            Email = email,
            Nome = $"Usuário {usuario}",
            OrganizacaoId = organizacaoId,
            Ativo = ativo,
            TrocaSenhaPendente = trocaSenhaPendente,
            CriadoEmUtc = agora,
            AtualizadoEmUtc = agora,
        };

        var criacao = await gerenciador.CreateAsync(conta, SenhaValida);
        Assert.IsTrue(criacao.Succeeded, Descrever(criacao));

        var atribuicao = await gerenciador.AddToRoleAsync(conta, papel);
        Assert.IsTrue(atribuicao.Succeeded, Descrever(atribuicao));

        return conta;
    }

    private static async Task<Result<SessaoEmitida>> IniciarAsync(
        string identificador,
        string senha = SenhaValida)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var handler = escopo.ServiceProvider.GetRequiredService<IniciarSessaoHandler>();

        return await handler.ExecutarAsync(
            new LoginEntrada(identificador, senha),
            CorrelacaoId);
    }

    private static async Task<Result<SessaoEmitida>> RenovarAsync(string refreshToken)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var handler = escopo.ServiceProvider.GetRequiredService<RenovarSessaoHandler>();

        return await handler.ExecutarAsync(refreshToken);
    }

    private static async Task<Result> EncerrarAsync(string refreshToken)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var handler = escopo.ServiceProvider.GetRequiredService<EncerrarSessaoHandler>();

        return await handler.ExecutarAsync(refreshToken, CorrelacaoId);
    }

    private static async Task<RefreshToken> ObterRefreshAsync(string valor)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
        var hash = RefreshTokenOpaco.CalcularHash(valor);

        return await contexto.RefreshTokens
            .AsNoTracking()
            .SingleAsync(token => token.Hash == hash);
    }

    private static async Task<List<RefreshToken>> ObterFamiliaAsync(Guid familiaId)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();

        return await contexto.RefreshTokens
            .AsNoTracking()
            .Where(token => token.FamiliaId == familiaId)
            .ToListAsync();
    }

    private static async Task<JsonWebToken> LerTokenValidadoAsync(string token)
    {
        var manipulador = new JsonWebTokenHandler();
        var validacao = await manipulador.ValidateTokenAsync(
            token,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Emissor,
                ValidateAudience = true,
                ValidAudience = Publico,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Chave)),
                ClockSkew = TimeSpan.Zero,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            });

        Assert.IsTrue(validacao.IsValid, validacao.Exception?.Message);

        return manipulador.ReadJsonWebToken(token);
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

    private static string Descrever(IdentityResult resultado)
    {
        return string.Join("; ", resultado.Errors.Select(erro => $"{erro.Code}: {erro.Description}"));
    }

    private static async Task InicializarAsync()
    {
        var connectionString = await SqlServerTestDatabase.ObterConnectionStringAsync(NomeBanco);

        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddApplication();
        servicos.AddInfrastructure(connectionString);
        servicos.AddSingleton(new JwtSettings(Emissor, Publico, Chave));
        servicos.AddSingleton(new ConfiguracaoDeSessao(
            TimeSpan.FromMinutes(15),
            TimeSpan.FromDays(7)));
        _provedor = servicos.BuildServiceProvider();

        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
        await contexto.Database.EnsureDeletedAsync();
        await contexto.Database.MigrateAsync();

        var gerenciadorPapeis = escopo.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        await PapelSeeder.GarantirPapeisAsync(gerenciadorPapeis);
    }

    private static async Task LimparAsync()
    {
        await using (var escopo = _provedor.CreateAsyncScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
            await contexto.Database.EnsureDeletedAsync();
        }

        await _provedor.DisposeAsync();
    }

    private sealed record ResultadoDeOrganizacao(Guid OrganizacaoId, Guid AdministradorId);
}
