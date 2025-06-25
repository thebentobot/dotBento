using dotBento.EntityFramework.Context;
using dotBento.WebApi;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Grafana.Loki;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

var configuration = builder.Configuration;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .WriteTo.GrafanaLoki(
        configuration["LokiUrl"] ?? "http://localhost:3100",
        labels: new[]
        {
            new LokiLabel { Key = "app", Value = "dotbento-webapi" },
            new LokiLabel { Key = "environment", Value = configuration["Environment"] ?? "development" }
        })
    .Enrich.WithExceptionDetails()
    .Enrich.WithProperty("Environment", configuration["Environment"] ?? "unknown")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddOpenApi();

var connectionString = configuration["DatabaseConnectionString"];
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("DatabaseConnectionString is not configured.");
}

builder.Services.AddDbContextFactory<BotDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.UseMetricServer();
app.UseHttpMetrics();
app.MapControllers();
app.Run();