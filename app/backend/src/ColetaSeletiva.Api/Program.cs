using System.Text;
using System.Text.Json.Serialization;
using ColetaSeletiva.Api.Configuration;
using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Application;
using ColetaSeletiva.Domain.Identidade;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseSerilog(
    (contexto, configuracao) =>
    {
        configuracao
            .ReadFrom.Configuration(contexto.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(new CompactJsonFormatter());
    },
    writeToProviders: true);

var jwtSettings = StartupConfiguration.CriarJwtSettings(builder.Configuration);
var configuracaoDeSessao = StartupConfiguration.CriarConfiguracaoDeSessao(builder.Configuration);
var limitesDeTaxa = StartupConfiguration.CriarConfiguracaoDeLimitesDeTaxa(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString(StartupConfiguration.ConnectionStringName)
    ?? throw new InvalidOperationException("A connection string da aplicação não foi configurada.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddExceptionHandler<TratadorGlobalDeExcecoes>();
builder.Services.AddProblemDetails();
builder.Services.AdicionarLimitadorDeTaxa(limitesDeTaxa);
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(configuracaoDeSessao);
builder.Services.AddSingleton(
    new ConfiguracaoDeOrigem(builder.Configuration["Sessao:OrigemPermitida"]));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
            RoleClaimType = "role",
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        PoliticasDeAcesso.SomenteAdministrador,
        policy => policy.RequireRole(Papeis.Administrador));
    options.AddPolicy(
        PoliticasDeAcesso.SomenteEquipeDaOrganizacao,
        policy => policy.RequireRole(Papeis.Administrador, Papeis.Operador));
    options.AddPolicy(
        PoliticasDeAcesso.SomenteCidadao,
        policy => policy.RequireRole(Papeis.Cidadao));
});
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0000} ms";
});
app.UseDefaultFiles();
app.UseStaticFiles();

// A SPA é publicada em wwwroot. Rotas de navegação sem extensão que não existem
// como arquivo recebem o index.html; /api mantém o 404 original.
var paginaInicialDaSpa = Path.Combine(app.Environment.WebRootPath ?? string.Empty, "index.html");
if (File.Exists(paginaInicialDaSpa))
{
    app.Use(async (contexto, proximo) =>
    {
        await proximo();

        if (contexto.Response.StatusCode == StatusCodes.Status404NotFound
            && !contexto.Response.HasStarted
            && HttpMethods.IsGet(contexto.Request.Method)
            && !contexto.Request.Path.StartsWithSegments("/api")
            && !contexto.Request.Path.StartsWithSegments("/health")
            && !contexto.Request.Path.StartsWithSegments("/swagger")
            && !contexto.Request.Path.StartsWithSegments("/openapi")
            && !Path.HasExtension(contexto.Request.Path)
            && contexto.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            contexto.Response.StatusCode = StatusCodes.Status200OK;
            contexto.Response.ContentType = "text/html; charset=utf-8";
            await contexto.Response.SendFileAsync(paginaInicialDaSpa);
        }
    });
}

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<PrimeiroAcessoMiddleware>();
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthorization();

app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "swagger";
    options.SwaggerEndpoint("/openapi/v1.json", "Coleta Seletiva API v1");
    options.DocumentTitle = "Coleta Seletiva API";
    options.DisplayRequestDuration();
});

if (app.Configuration.GetValue("Database:AplicarMigrationsNoInicio", false))
{
    await using var escopoDeMigracao = app.Services.CreateAsyncScope();
    var banco = escopoDeMigracao.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
    await banco.Database.MigrateAsync();
}

await using (var escopo = app.Services.CreateAsyncScope())
{
    var gerenciadorPapeis = escopo.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    await PapelSeeder.GarantirPapeisAsync(gerenciadorPapeis);
}

app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/v1", (HttpRequest requisicao) =>
{
    var aceitaHtml = requisicao.Headers.Accept.ToString()
        .Contains("text/html", StringComparison.OrdinalIgnoreCase);

    if (aceitaHtml)
    {
        return Results.Redirect("/swagger");
    }

    return Results.Ok(new
    {
        status = "ok",
        mensagem = "API Coleta Seletiva",
        documentacao = "/swagger",
        openapi = "/openapi/v1.json",
        health = "/health",
    });
});
app.MapControllers();

app.Run();

public partial class Program
{
}
