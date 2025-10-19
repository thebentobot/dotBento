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
// Keep IMemoryCache for local, per-process caching used elsewhere
builder.Services.AddMemoryCache();

// Configure shared distributed cache (Redis). Fail fast if no connection string.
var redisConnection = configuration["Redis:ConnectionString"] ?? configuration["RedisConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(opts =>
    {
        opts.Configuration = redisConnection;
        opts.InstanceName = "dotbento:";
    });
}
else
{
    throw new InvalidOperationException("Redis connection string is not configured. Set either Redis:ConnectionString (env: REDIS__CONNECTIONSTRING) or RedisConnectionString (env: REDISCONNECTIONSTRING) to enable shared cache.");
}

builder.Services.AddOpenApi();

builder.Services.AddScoped<dotBento.Infrastructure.Services.ProfileService>();

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