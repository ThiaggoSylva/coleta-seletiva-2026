using System.Text;
using System.Text.Json.Serialization;
using ColetaSeletiva.Api.Configuration;
using ColetaSeletiva.Api.Erros;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Application;
using ColetaSeletiva.Domain.Identidade;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Identidade;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
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
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<PrimeiroAcessoMiddleware>();
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthorization();

await using (var escopo = app.Services.CreateAsyncScope())
{
    var gerenciadorPapeis = escopo.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    await PapelSeeder.GarantirPapeisAsync(gerenciadorPapeis);
}

app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();

public partial class Program
{
}
