using System.Reflection;
using BotListAPI;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Handlers;
using dotBento.Bot.Interfaces;
using dotBento.Domain;
using dotBento.EntityFramework.Context;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Prometheus;
using Serilog;

namespace dotBento.Bot.Services;

public class BotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly CommandService _commands;
    private readonly IConfiguration _config;
    private readonly IServiceProvider _provider;
    private readonly InteractionHandler _interactionHandler;
    private readonly IDbContextFactory<BotDbContext> _contextFactory;
    private readonly UserService _userService;
    private readonly GuildService _guildService;
    private readonly MessageHandler _messageHandler;
    private readonly IPrefixService _prefixService;
    private readonly BackgroundService _backgroundService;

    public BotService(DiscordSocketClient client,
        InteractionService interactions,
        IConfiguration config,
        InteractionHandler interactionHandler,
        IDbContextFactory<BotDbContext> contextFactory,
        UserService userService,
        GuildService guildService,
        MessageHandler messageHandler,
        IPrefixService prefixService,
        CommandService commands,
        IServiceProvider provider,
        BackgroundService backgroundService)
    {
        _client = client;
        _interactions = interactions;
        _config = config;
        _interactionHandler = interactionHandler;
        _contextFactory = contextFactory;
        _userService = userService;
        _guildService = guildService;
        _messageHandler = messageHandler;
        _prefixService = prefixService;
        _commands = commands;
        _provider = provider;
        _backgroundService = backgroundService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
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
        
        Log.Information("Loading all prefixes");
        await _prefixService.LoadAllPrefixes();
        
        _client.Ready += ClientReady;
        
        Log.Information("Starting bot");
        
        Log.Information("Loading command modules");
        await _commands
            .AddModulesAsync(
                Assembly.GetEntryAssembly(),
                _provider);

        Log.Information("Loading interaction modules");
        /*
        await _interactions
            .AddModulesAsync(
                Assembly.GetEntryAssembly(),
                _provider);
        */
        await _interactionHandler.InitializeAsync();
        
        Log.Information("Preparing cache folder");
        PrepareCacheFolder();
        
        Log.Information("Logging into Discord");
        await _client.LoginAsync(TokenType.Bot, _config["Discord:BotToken"]);

        await _client.StartAsync();
        
        await _backgroundService.UpdateMetrics();

        InitializeHangfireConfig();
        _backgroundService.QueueJobs();
        
        StartMetricsPusher();
        
        BackgroundJob.Schedule(() => RegisterSlashCommands(), TimeSpan.FromSeconds(10));
        BackgroundJob.Schedule(() => CacheSlashCommandIds(), TimeSpan.FromSeconds(10));
        BackgroundJob.Schedule(() => StartBotSiteUpdater(), TimeSpan.FromSeconds(10));
        
        await CacheDiscordUserIds();
    }

    public void StartBotSiteUpdater()
    {
        if (!_client.CurrentUser.Id.Equals(Constants.BotProductionId))
        {
            Log.Information("Cancelled botlist updater, non-production bot detected");
            return;
        }

        Log.Information("Starting botlist updater");

        var listConfig = new ListConfig
        {
            TopGG = _config["TopGG:Token"]
        };

        try
        {
            var listClient = new ListClient(_client, listConfig);

            listClient.Start();
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception while attempting to start botlist updater!");
        }
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
        await _interactions.RegisterCommandsToGuildAsync(Constants.BotDevelopmentId);
#else
        Log.Information("Registering slash commands globally");
        await _interactionService.RegisterCommandsGloballyAsync();
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
        var commands = await _client.Rest.GetGlobalApplicationCommands();
        Log.Information("Found {slashCommandCount} registered slash commands", commands.Count);

        foreach (var cmd in commands)
        {
            PublicProperties.SlashCommands.TryAdd(cmd.Name, cmd.Id);
        }
    }
    
    private async Task CacheDiscordUserIds()
    {
        var users = await _userService.GetAllDiscordUserIds();
        Log.Information("Found {slashCommandCount} registered users", users.Count);

        foreach (var user in users)
        {
            PublicProperties.RegisteredUsers.TryAdd((ulong)user.UserId, (int)user.UserId);
        }
    }
    
    private void InitializeHangfireConfig()
    {
        GlobalConfiguration.Configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSerilogLogProvider()
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseMemoryStorage()
            .UseActivator(new HangfireActivator(_provider));
    }
}