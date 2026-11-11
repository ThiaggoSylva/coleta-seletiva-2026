using ColetaSeletiva.Api.Configuration;
using ColetaSeletiva.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

StartupConfiguration.Validate(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString(StartupConfiguration.ConnectionStringName)
    ?? throw new InvalidOperationException("A connection string da aplicação não foi configurada.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();

public partial class Program
{
}
