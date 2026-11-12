using System.Text.Json.Serialization;
using ColetaSeletiva.Api.Configuration;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Application;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Identidade;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

StartupConfiguration.Validate(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString(StartupConfiguration.ConnectionStringName)
    ?? throw new InvalidOperationException("A connection string da aplicação não foi configurada.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<TenantContextMiddleware>();

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
