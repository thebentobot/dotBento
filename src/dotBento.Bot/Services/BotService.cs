using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Logging;
using dotBento.Bot.Models;
using dotBento.EntityFramework.Context;
using dotBento.Infrastructure.Interfaces;
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
    IPrefixService prefixService,
    CommandService commands,
    IServiceProvider provider,
    BackgroundService backgroundService,
    IOptions<BotEnvConfig> config)
{
    private MetricPusher? _metricPusher;

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

        // TODO: Text commands are disabled because the bot does not have the MessageContent intent.
        // Re-enable these (and the command parsing in MessageHandler) when the intent is granted.
        // Log.Information("Loading all prefixes");
        // await prefixService.LoadAllPrefixes();

        Log.Information("Starting bot");
        var discordToken = config.Value.Discord.Token ??
                           throw new InvalidOperationException("Discord:Token environment variable not set.");

        // TODO: Re-enable when MessageContent intent is granted (see above).
        // Log.Information("Loading command modules");
        // await commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider);

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

        client.Ready += async () =>
        {
            Log.Information("Client Ready - Registering slash commands and initializing bot site updater");

            // Activate Discord channel logging sink now that client is ready
            DiscordChannelSinkExtensions.ActivateDiscordChannelSink(client);

            await RegisterSlashCommands();
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
        _metricPusher = new MetricPusher(new MetricPusherOptions
        {
            Endpoint = metricsPusherEndpoint,
            Job = metricsPusherName
        });

        _metricPusher.Start();

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
