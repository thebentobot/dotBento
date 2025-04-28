using System.Reflection;
using BotListAPI;
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
    
        StartMetricsServer();

        await CacheDiscordUserIds();

        client.Ready += async () =>
        {
            Log.Information("Client Ready - Registering slash commands and initializing bot site updater");
            await RegisterSlashCommands();
            await CacheSlashCommandIds();
            StartBotSiteUpdater();
        };
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
    
    private void StartMetricsServer()
    {
        Log.Information("Starting Prometheus Metric Server on port 8080");
        var metricServer = new KestrelMetricServer(port: 8080);
        metricServer.Start();
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