using System.Reflection;
using BotListAPI;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Handlers;
using dotBento.Bot.Interfaces;
using dotBento.Bot.Models;
using dotBento.Domain;
using dotBento.EntityFramework.Context;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Prometheus;
using Serilog;

namespace dotBento.Bot.Services;

public class BotService(DiscordSocketClient client,
    InteractionService interactions,
    IDbContextFactory<BotDbContext> contextFactory,
    UserService userService,
    IPrefixService prefixService,
    CommandService commands,
    IServiceProvider provider,
    BackgroundService backgroundService,
    IOptions<BotEnvConfig> config)
{
    
    public async Task StartAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            Log.Information("Ensuring database is up to date");
            await context.Database.MigrateAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Something went wrong while creating/updating the database!");
            throw;
        }
        
        Log.Information("Loading all prefixes");
        await prefixService.LoadAllPrefixes();
        
        client.Ready += ClientReady;
        
        Log.Information("Starting bot");
        
        Log.Information("Loading command modules");
        await commands
            .AddModulesAsync(
                Assembly.GetEntryAssembly(),
                provider);

        Log.Information("Loading interaction modules");
        
        await interactions
            .AddModulesAsync(
                Assembly.GetEntryAssembly(),
                provider);
        
        Log.Information("Preparing cache folder");
        PrepareCacheFolder();
        
        Log.Information("Logging into Discord");
        await client.LoginAsync(TokenType.Bot, config.Value.Discord.Token);

        await client.StartAsync();
        
        await backgroundService.UpdateMetrics();

        InitializeHangfireConfig();
        backgroundService.QueueJobs();
        
        StartMetricsPusher();
        
        BackgroundJob.Schedule(() => RegisterSlashCommands(), TimeSpan.FromSeconds(10));
        BackgroundJob.Schedule(() => CacheSlashCommandIds(), TimeSpan.FromSeconds(10));
        BackgroundJob.Schedule(() => StartBotSiteUpdater(), TimeSpan.FromSeconds(10));
        
        await CacheDiscordUserIds();
    }

    // public instead of private because of Hangfire BackgroundJob
    // ReSharper disable once MemberCanBePrivate.Global
    public void StartBotSiteUpdater()
    {
        if (!client.CurrentUser.Id.Equals(Constants.BotProductionId))
        {
            Log.Information("Cancelled botlist updater, non-production bot detected");
            return;
        }

        Log.Information("Starting botlist updater");

        var listConfig = new ListConfig
        {
            TopGG = config.Value.BotLists.TopGgApiToken
        };

        try
        {
            var listClient = new ListClient(client, listConfig);

            listClient.Start();
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception while attempting to start botlist updater!");
        }
    }

    public async Task StopAsync()
    {
        await client.StopAsync();
    }

    private async Task ClientReady()
    {
        Log.Information("Logged as {ClientCurrentUser}", client.CurrentUser);

        await interactions.RegisterCommandsGloballyAsync();
    }
    
    // public instead of private because of Hangfire BackgroundJob
    // ReSharper disable once MemberCanBePrivate.Global
    public async Task RegisterSlashCommands()
    {
        Log.Information("Starting slash command registration");

#if DEBUG
        Log.Information("Registering slash commands to guild");
        await interactions.RegisterCommandsToGuildAsync(Constants.BotDevelopmentId);
#else
        Log.Information("Registering slash commands globally");
        await interactions.RegisterCommandsGloballyAsync();
#endif
    }
    
    private void StartMetricsPusher()
    {
        string metricsPusherEndpoint = config.Value.Prometheus.MetricsPusherEndpoint ??
                                       throw new InvalidOperationException(
                                           "MetricsPusherEndpoint environment variable are not set.");
        string metricsPusherName = config.Value.Prometheus.MetricsPusherName ??
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

    // public instead of private because of Hangfire BackgroundJob
    // ReSharper disable once MemberCanBePrivate.Global
    public async Task CacheSlashCommandIds()
    {
        var globalApplicationCommands = await client.Rest.GetGlobalApplicationCommands();
        Log.Information("Found {SlashCommandCount} registered slash commands", globalApplicationCommands.Count);

        foreach (var cmd in globalApplicationCommands)
        {
            PublicProperties.SlashCommands.TryAdd(cmd.Name, cmd.Id);
        }
    }
    
    private async Task CacheDiscordUserIds()
    {
        var users = await userService.GetAllDiscordUserIds();
        Log.Information("Found {SlashCommandCount} registered users", users.Count);

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
            .UseActivator(new HangfireActivator(provider));
    }
}