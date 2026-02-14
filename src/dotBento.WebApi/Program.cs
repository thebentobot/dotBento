using dotBento.EntityFramework.Context;
using dotBento.WebApi;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Grafana.Loki;

// Load .env file if it exists (for local development)
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

var configuration = builder.Configuration;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .ReadFrom.Configuration(configuration)
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
var distributedCacheConnection = configuration["Valkey:ConnectionString"]
    ?? configuration["Redis:ConnectionString"]
    ?? configuration["RedisConnectionString"];
if (!string.IsNullOrWhiteSpace(distributedCacheConnection))
{
    builder.Services.AddStackExchangeRedisCache(opts =>
    {
        opts.Configuration = distributedCacheConnection;
        opts.InstanceName = "dotbento:";
    });
}
else
{
    throw new InvalidOperationException("Valkey/Redis connection string is not configured. Set Valkey:ConnectionString (env: VALKEY__CONNECTIONSTRING) or Redis:ConnectionString (env: REDIS__CONNECTIONSTRING) or RedisConnectionString (env: REDISCONNECTIONSTRING) to enable the shared cache.");
}

builder.Services.AddOpenApi();

builder.Services.AddScoped<dotBento.Infrastructure.Services.ProfileService>();
builder.Services.AddScoped<dotBento.Infrastructure.Services.LeaderboardService>();
builder.Services.AddScoped<dotBento.Infrastructure.Services.GuildSettingService>();
builder.Services.AddScoped<dotBento.Infrastructure.Services.UserSettingService>();

var discordToken = configuration["Discord:Token"]
    ?? throw new InvalidOperationException(
        "Discord:Token is not configured. Set Discord__Token environment variable.");

builder.Services.AddHttpClient<dotBento.Infrastructure.Services.Api.DiscordApiService>(client =>
{
    client.BaseAddress = new Uri("https://discord.com/api/v10/");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bot", discordToken);
    client.Timeout = TimeSpan.FromSeconds(10);
});

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

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.UseMetricServer();
app.UseHttpMetrics();
app.MapControllers();
app.Run();