using System.Reflection;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;
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

#pragma warning disable CS9113 // text-command params kept for re-enablement; see TODO at StartAsync:44
public sealed class BotService(GatewayClient client,
    ApplicationCommandService<ApplicationCommandContext, AutocompleteInteractionContext> interactions,
    NetCord.Services.ComponentInteractions.ComponentInteractionService<NetCord.Services.ComponentInteractions.ComponentInteractionContext> componentInteractions,
    NetCord.Services.ComponentInteractions.ComponentInteractionService<NetCord.Services.ComponentInteractions.ModalInteractionContext> modalInteractions,
    IDbContextFactory<BotDbContext> contextFactory,
    IPrefixService prefixService,
    CommandService<CommandContext> commands,
    IServiceProvider provider,
    BackgroundService backgroundService,
    IOptions<BotEnvConfig> config)
#pragma warning restore CS9113
{
    private const ulong DefaultDevelopmentGuildId = 790353119795871744UL;
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

        // Log.Information("Loading command modules");
        // commands.AddModules(Assembly.GetEntryAssembly()!);

        Log.Information("Starting bot");

        Log.Information("Loading interaction modules");
        RegisterInteractionModules(interactions, componentInteractions, modalInteractions, Assembly.GetEntryAssembly()!);

        Log.Information("Preparing cache folder");
        PrepareCacheFolder();

        client.Ready += OnReadyAsync;

        Log.Information("Connecting to Discord");
        await client.StartAsync();

        await backgroundService.UpdateMetrics();
        InitializeHangfireConfig();
        backgroundService.QueueJobs();

        StartMetricsPusher();
    }

    public static void RegisterInteractionModules(
        ApplicationCommandService<ApplicationCommandContext, AutocompleteInteractionContext> applicationCommands,
        NetCord.Services.ComponentInteractions.ComponentInteractionService<NetCord.Services.ComponentInteractions.ComponentInteractionContext> componentInteractions,
        NetCord.Services.ComponentInteractions.ComponentInteractionService<NetCord.Services.ComponentInteractions.ModalInteractionContext> modalInteractions,
        Assembly assembly)
    {
        applicationCommands.AddModules(assembly);
        componentInteractions.AddModules(assembly);
        modalInteractions.AddModules(assembly);
    }

    private async ValueTask OnReadyAsync(ReadyEventArgs args)
    {
        Log.Information("Client Ready - Registering slash commands and initializing bot site updater");

        // Activate Discord channel logging sink now that client is ready
        DiscordChannelSinkExtensions.ActivateDiscordChannelSink(client);

        await RegisterSlashCommands();
    }

    // public instead of private because of Hangfire BackgroundJob
    // ReSharper disable once MemberCanBePrivate.Global
    public async Task RegisterSlashCommands()
    {
        var applicationId = client.Cache.User?.Id ?? throw new InvalidOperationException("Bot user ID not available");
        var commandsToRegister = await GetRawApplicationCommandsAsync(interactions);
        Log.Information("Starting slash command registration");

#if DEBUG
        var developmentGuildId = config.Value.Discord.DevelopmentGuildId is 0
            ? DefaultDevelopmentGuildId
            : config.Value.Discord.DevelopmentGuildId;
        Log.Information("Bulk overwriting slash commands to development guild {GuildId}", developmentGuildId);
        var registered = await client.Rest.BulkOverwriteGuildApplicationCommandsAsync(
            applicationId,
            developmentGuildId,
            commandsToRegister);
#else
        Log.Information("Bulk overwriting slash commands globally");
        var registered = await client.Rest.BulkOverwriteGlobalApplicationCommandsAsync(
            applicationId,
            commandsToRegister);
#endif
        foreach (var cmd in registered)
        {
            Log.Information("Registered command: {Name}", cmd.Name);
            LogRegisteredOptions(cmd.Options, "  ");
        }
    }

    private static async Task<IReadOnlyList<ApplicationCommandProperties>> GetRawApplicationCommandsAsync(
        ApplicationCommandService<ApplicationCommandContext, AutocompleteInteractionContext> applicationCommands)
    {
        var result = new List<ApplicationCommandProperties>();

        foreach (var command in applicationCommands.GetCommands())
        {
            result.Add(await GetRawApplicationCommandAsync(command));
        }

        return result;
    }

    private static async Task<ApplicationCommandProperties> GetRawApplicationCommandAsync(object command)
    {
        var method = command.GetType().GetMethod(
            "GetRawValueAsync",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"{command.GetType().FullName} does not expose GetRawValueAsync.");

        var rawTask = method.Invoke(command, [CancellationToken.None])
            ?? throw new InvalidOperationException("GetRawValueAsync returned null.");
        dynamic awaitable = rawTask;
        return (ApplicationCommandProperties)await awaitable;
    }

    private static void LogRegisteredOptions(IEnumerable<ApplicationCommandOption>? options, string indent)
    {
        if (options is null)
        {
            return;
        }

        foreach (var option in options)
        {
            Log.Debug(
                "{Indent}option: {Name} ({Type}) required={Required}",
                indent,
                option.Name,
                option.Type,
                option.Required);
            LogRegisteredOptions(option.Options, indent + "  ");
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
