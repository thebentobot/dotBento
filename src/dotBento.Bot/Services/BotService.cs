using System.Reflection;
using NetCord;
using NetCord.Gateway;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;
using dotBento.Bot.Logging;
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

public sealed class BotService(GatewayClient client,
    ApplicationCommandService<ApplicationCommandContext, AutocompleteInteractionContext> interactions,
    IDbContextFactory<BotDbContext> contextFactory,
    UserService userService,
    IPrefixService prefixService,
    CommandService<CommandContext> commands,
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
        commands.AddModules(Assembly.GetEntryAssembly()!);

        Log.Information("Loading interaction modules");
        interactions.AddModules(Assembly.GetEntryAssembly()!);

        Log.Information("Preparing cache folder");
        PrepareCacheFolder();

        client.Ready += OnReadyAsync;

        Log.Information("Connecting to Discord");
        await client.StartAsync();

        await backgroundService.UpdateMetrics();
        InitializeHangfireConfig();
        backgroundService.QueueJobs();

        StartMetricsPusher();

        await CacheDiscordUserIds();
    }

    private async ValueTask OnReadyAsync(ReadyEventArgs args)
    {
        Log.Information("Client Ready - Registering slash commands and initializing bot site updater");

        // Activate Discord channel logging sink now that client is ready
        DiscordChannelSinkExtensions.ActivateDiscordChannelSink(client);

        await RegisterSlashCommands();
        await CacheSlashCommandIds();
    }

    // public instead of private because of Hangfire BackgroundJob
    // ReSharper disable once MemberCanBePrivate.Global
    public async Task RegisterSlashCommands()
    {
        var applicationId = client.Cache.User?.Id ?? throw new InvalidOperationException("Bot user ID not available");
        Log.Information("Starting slash command registration");

#if DEBUG
        Log.Information("Registering slash commands to guild");
        // TODO: Make this an env var for development discord server
        var registered = await interactions.RegisterCommandsAsync(client.Rest, applicationId, guildId: 790353119795871744UL);
#else
        Log.Information("Registering slash commands globally");
        var registered = await interactions.RegisterCommandsAsync(client.Rest, applicationId);
#endif
        foreach (var cmd in registered)
        {
            Log.Information("Registered command: {Name}", cmd.Name);
            foreach (var opt in cmd.Options ?? [])
                Log.Debug("  option: {Name} ({Type})", opt.Name, opt.Type);
        }
    }

    private void StartMetricsPusher()
    {
        var environment = config.Value.Environment;

        // Only push metrics in production or staging environments
        if (!environment.Equals("production", StringComparison.OrdinalIgnoreCase) &&
            !environment.Equals("staging", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("Skipping metrics pusher in {Environment} environment", environment);
            return;
        }

        var metricsPusherEndpoint = config.Value.Prometheus.MetricsPusherEndpoint;
        var metricsPusherName = config.Value.Prometheus.MetricsPusherName;

        if (string.IsNullOrEmpty(metricsPusherEndpoint) || string.IsNullOrEmpty(metricsPusherName))
        {
            Log.Warning("Metrics pusher not configured - MetricsPusherEndpoint or MetricsPusherName is empty");
            return;
        }

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
            var applicationId = client.Cache.User?.Id ?? throw new InvalidOperationException("Bot user ID not available");
#if DEBUG
            var globalApplicationCommands = await client.Rest.GetGuildApplicationCommandsAsync(applicationId, 790353119795871744UL);
#else
            var globalApplicationCommands = await client.Rest.GetGlobalApplicationCommandsAsync(applicationId);
#endif
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
        catch (NetCord.Rest.RestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
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
