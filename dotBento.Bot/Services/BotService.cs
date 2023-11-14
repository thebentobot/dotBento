using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Handlers;
using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace dotBento.Bot.Services;

public class BotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IConfiguration _config;
    private readonly ILogger _logger;
    private readonly InteractionHandler _interactionHandler;
    private readonly IDbContextFactory<BotDbContext> _contextFactory;

    public BotService(DiscordSocketClient client,
        InteractionService interactions,
        IConfiguration config,
        ILogger<BotService> logger,
        InteractionHandler interactionHandler,
        IDbContextFactory<BotDbContext> contextFactory)
    {
        _client = client;
        _interactions = interactions;
        _config = config;
        _logger = logger;
        _interactionHandler = interactionHandler;
        _contextFactory = contextFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var context = await this._contextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            Log.Information("Ensuring database is up to date");
            await context.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception e)
        {
            Log.Error(e, "Something went wrong while creating/updating the database!");
            throw;
        }
        _client.Ready += ClientReady;

        _client.Log += LogAsync;
        _interactions.Log += LogAsync;
        
        Log.Information("Starting bot");

        await _interactionHandler.InitializeAsync();
        
        Log.Information("Logging into Discord");
        await _client.LoginAsync(TokenType.Bot, _config["Discord:BotToken"]);

        await _client.StartAsync();
        
        this.StartMetricsPusher();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
    }

    private async Task ClientReady()
    {
        _logger.LogInformation($"Logged as {_client.CurrentUser}");

        await _interactions.RegisterCommandsGloballyAsync();
    }

    private async Task LogAsync(LogMessage msg)
    {
        var severity = msg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Trace,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Information
        };

        _logger.Log(severity, msg.Exception, msg.Message);

        await Task.CompletedTask;
    }
    
    public async Task RegisterSlashCommands()
    {
        Log.Information("Starting slash command registration");

#if DEBUG
        Log.Information("Registering slash commands to guild");
        //await this._interactions.RegisterCommandsToGuildAsync(this._botSettings.Bot.BaseServerId);
#else
            Log.Information("Registering slash commands globally");
            await this._interactionService.RegisterCommandsGloballyAsync();
#endif
    }
    
    private void StartMetricsPusher()
    {
        string metricsPusherEndpoint = _config["Prometheus:MetricsPusherEndpoint"] ??
                                       throw new InvalidOperationException(
                                           "MetricsPusherEndpoint environment variable are not set.");
        string metricsPusherName = _config["Prometheus:MetricsPusherName"] ??
                                   throw new InvalidOperationException(
                                       "MetricsPusherName environment variable are not set.");

        Log.Information("Starting metrics pusher");
        var pusher = new MetricPusher(new MetricPusherOptions
        {
            Endpoint = metricsPusherEndpoint,
            Job = metricsPusherName
        });

        pusher.Start();

        Log.Information("Metrics pusher pushing to {MetricsPusherEndpoint}, job name {MetricsPusherName}", metricsPusherEndpoint, metricsPusherName);
    }
}