using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Handlers;
using dotBento.Domain;
using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using Serilog;

namespace dotBento.Bot.Services;

public class BotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IConfiguration _config;
    private readonly InteractionHandler _interactionHandler;
    private readonly IDbContextFactory<BotDbContext> _contextFactory;
    private readonly UserService _userService;

    public BotService(DiscordSocketClient client,
        InteractionService interactions,
        IConfiguration config,
        ILogger<BotService> logger,
        InteractionHandler interactionHandler,
        IDbContextFactory<BotDbContext> contextFactory, UserService userService)
    {
        _client = client;
        _interactions = interactions;
        _config = config;
        _interactionHandler = interactionHandler;
        _contextFactory = contextFactory;
        _userService = userService;
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
        Log.Information($"Logged as {_client.CurrentUser}");

        await _interactions.RegisterCommandsGloballyAsync();
    }
    
    public async Task RegisterSlashCommands()
    {
        Log.Information("Starting slash command registration");

#if DEBUG
        Log.Information("Registering slash commands to guild");
        //TODO lav en base server id i settings
        //await this._interactions.RegisterCommandsToGuildAsync(this._botSettings.Bot.BaseServerId);
        // der findes også en overwrite registercommands metode
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
    
    private static void PrepareCacheFolder()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public async Task CacheSlashCommandIds()
    {
        var commands = await this._client.Rest.GetGlobalApplicationCommands();
        Log.Information("Found {slashCommandCount} registered slash commands", commands.Count);

        foreach (var cmd in commands)
        {
            PublicProperties.SlashCommands.TryAdd(cmd.Name, cmd.Id);
        }
    }
    
    private async Task CacheDiscordUserIds()
    {
        var users = await this._userService.GetAllDiscordUserIds();
        Log.Information("Found {slashCommandCount} registered users", users.Count);

        foreach (var user in users)
        {
            PublicProperties.RegisteredUsers.TryAdd((ulong)user.UserId, (int)user.UserId);
        }
    }
}