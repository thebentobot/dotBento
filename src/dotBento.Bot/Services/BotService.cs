using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Models;
using dotBento.Domain;
using dotBento.EntityFramework.Context;
using dotBento.Infrastructure.Interfaces;
using dotBento.Infrastructure.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Prometheus;
using Serilog;

namespace dotBento.Bot.Services;

public sealed class BotService(DiscordSocketClient client,
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
    
        Log.Information("Starting bot");
        var discordToken = config.Value.Discord.Token ??
                           throw new InvalidOperationException("Discord:Token environment variable not set.");
    
        Log.Information("Loading command modules");
        await commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider);

        Log.Information("Loading interaction modules");
        await interactions.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
    
        Log.Information("Preparing cache folder");
        PrepareCacheFolder();
    
        Log.Information("Logging into Discord");
        await client.LoginAsync(TokenType.Bot, discordToken);
    
        await client.StartAsync();
    
        await backgroundService.UpdateMetrics();
        InitializeHangfireConfig();
        backgroundService.QueueJobs();
    
        StartMetricsPusher();

        await CacheDiscordUserIds();

        client.Ready += async () =>
        {
            Log.Information("Client Ready - Registering slash commands and initializing bot site updater");
            await RegisterSlashCommands();
            await CacheSlashCommandIds();
        };
    }

    // public instead of private because of Hangfire BackgroundJob
    // ReSharper disable once MemberCanBePrivate.Global
    public async Task RegisterSlashCommands()
    {
        Log.Information("Starting slash command registration");

#if DEBUG
        Log.Information("Registering slash commands to guild");
        // TODO: Make this an env var for development discord server
        await interactions.RegisterCommandsToGuildAsync(790353119795871744);
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
        try
        {
            var globalApplicationCommands = await client.Rest.GetGlobalApplicationCommands();
            Log.Information("Found {SlashCommandCount} registered slash commands", globalApplicationCommands.Count);

            foreach (var cmd in globalApplicationCommands)
            {
                // Ensure each command ID is valid before adding it
                if (ulong.TryParse(cmd.Id.ToString(), out var commandId))
                {
                    PublicProperties.SlashCommands.TryAdd(cmd.Name, commandId);
                }
                else
                {
                    Log.Warning("Command ID {CommandId} for {CommandName} is not a valid snowflake", cmd.Id, cmd.Name);
                }
            }
        }
        catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.BadRequest)
        {
            Log.Error("Invalid Form Body: Check command ID formatting or other parameters. Details: {Exception}", ex);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception occurred while caching slash command IDs");
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