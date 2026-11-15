using System.Text.Json;
using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ColetaSeletiva.IntegrationTests.Erros;

[TestClass]
public sealed class TratadorGlobalDeExcecoesTests
{
    [TestMethod]
    public async Task Excecao_nao_tratada_retorna_problemdetails_generico_sem_pii()
    {
        var coletor = new ColetorDeLogs();
        using var fabricaDeLogs = LoggerFactory.Create(logging => logging.AddProvider(coletor));
        var tratador = new TratadorGlobalDeExcecoes(
            fabricaDeLogs.CreateLogger<TratadorGlobalDeExcecoes>());

        var contexto = new DefaultHttpContext();
        contexto.Request.Method = "POST";
        contexto.Request.Path = "/api/v1/auth/login";
        contexto.TraceIdentifier = "correlacao-de-teste";
        contexto.Response.Body = new MemoryStream();

        var tratado = await tratador.TryHandleAsync(
            contexto,
            new InvalidOperationException("senha=Segredo123 email=fulano@example.org"),
            CancellationToken.None);

        Assert.IsTrue(tratado);
        Assert.AreEqual(StatusCodes.Status500InternalServerError, contexto.Response.StatusCode);
        Assert.AreEqual("application/problem+json", contexto.Response.ContentType);

        contexto.Response.Body.Position = 0;
        using var corpo = await JsonDocument.ParseAsync(contexto.Response.Body);
        var raiz = corpo.RootElement;

        Assert.AreEqual(500, raiz.GetProperty("status").GetInt32());
        Assert.AreEqual("erro_interno", raiz.GetProperty("code").GetString());
        Assert.AreEqual("Não foi possível concluir a operação.", raiz.GetProperty("title").GetString());
        Assert.AreEqual("correlacao-de-teste", raiz.GetProperty("traceId").GetString());
        Assert.IsFalse(raiz.TryGetProperty("detail", out _));

        var logs = coletor.ObterMensagens();
        Assert.IsTrue(logs.Any(mensagem =>
            mensagem.Contains("InvalidOperationException", StringComparison.Ordinal)));
        Assert.IsFalse(logs.Any(mensagem =>
            mensagem.Contains("Segredo123", StringComparison.Ordinal)));
        Assert.IsFalse(logs.Any(mensagem =>
            mensagem.Contains("fulano@example.org", StringComparison.Ordinal)));
    }
}
