using ColetaSeletiva.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);

StartupConfiguration.Validate(builder.Configuration);

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
