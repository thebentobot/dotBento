using System.Reflection;
using System.Runtime.CompilerServices;
using DotNetEnv;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.TypeReaders;
using dotBento.Bot.Handlers;
using dotBento.Bot.Logging;
using dotBento.Bot.Models;
using dotBento.Bot.Services;
using dotBento.EntityFramework.Context;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Interfaces;
using dotBento.Infrastructure.Services;
using dotBento.Infrastructure.Services.Api;
using dotBento.Infrastructure.Utilities;
using Fergun.Interactive;
using Ganss.Xss;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;
using NetCord.Services.ComponentInteractions;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Discord;
using Serilog.Sinks.Grafana.Loki;
using BackgroundService = dotBento.Bot.Services.BackgroundService;

namespace dotBento.Bot;

public sealed class Startup
{
    private IConfiguration Configuration { get; }

    public Startup()
    {
        // Find repo root by looking for .env file traversing up from assembly location
        var envPath = FindEnvFile();
        if (envPath != null)
        {
            Env.Load(envPath);
        }

        Configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
    }

    private static string? FindEnvFile()
    {
        // Start from the assembly location and traverse up to find .env
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            var envPath = Path.Combine(directory.FullName, ".env");
            if (File.Exists(envPath))
            {
                return envPath;
            }
            directory = directory.Parent;
        }

        return null;
    }

    public static async Task RunAsync(string[] args)
    {
        var startup = new Startup();

        await startup.RunAsync();
    }

    private async Task RunAsync()
    {
        var environment = Configuration["Environment"] ?? "local";
        var isProduction = environment.Equals("production", StringComparison.OrdinalIgnoreCase);
        var isStaging = environment.Equals("staging", StringComparison.OrdinalIgnoreCase);

        // Logging levels based on environment:
        //   production - Warning console, Information minimum
        //   staging    - Verbose console, Verbose minimum
        //   local      - Verbose console, Verbose minimum
        var (consoleLevel, logLevel) = isProduction
            ? (LogEventLevel.Warning, LogEventLevel.Information)
            : (LogEventLevel.Verbose, LogEventLevel.Verbose);

#if DEBUG
        // Always use verbose logging in debug builds
        consoleLevel = LogEventLevel.Verbose;
        logLevel = LogEventLevel.Verbose;
#endif

        var loggerConfig = new LoggerConfiguration()
            .WriteTo.Console(consoleLevel)
            .MinimumLevel.Is(logLevel)
            .Enrich.WithProperty("Environment", environment)
            .Enrich.WithExceptionDetails();

        // Add Loki sink in production or staging when URL is configured
        var lokiUrl = Configuration["LokiUrl"];
        if ((isProduction || isStaging) && !string.IsNullOrEmpty(lokiUrl))
        {
            loggerConfig.WriteTo.GrafanaLoki(
                lokiUrl,
                labels:
                [
                    new LokiLabel { Key = "app", Value = "dotbento-bot" },
                    new LokiLabel { Key = "environment", Value = environment }
                ]
            );
        }

        // Only add Discord webhook sink when configured
        var discordWebhookId = Configuration["Discord:LogWebhookId"];
        var discordWebhookToken = Configuration["Discord:LogWebhookToken"];
        if (!string.IsNullOrEmpty(discordWebhookId) && !string.IsNullOrEmpty(discordWebhookToken))
        {
            loggerConfig.WriteTo.Discord(Convert.ToUInt64(discordWebhookId), discordWebhookToken);
        }

        // Add Discord channel sink when configured (deferred until client is ready)
        // Uses the same log level as the console (Warning in production, Verbose locally)
        var logChannelId = Configuration.GetValue<ulong>("Bot:LogChannelId");
        if (logChannelId != 0)
        {
            loggerConfig.WriteTo.DiscordChannel(logChannelId, consoleLevel);
        }

        Log.Logger = loggerConfig.CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += AppUnhandledException;

        Log.Information("dotBento is starting up...");

        var services = new ServiceCollection();
        ConfigureServices(services);

        var provider = services.BuildServiceProvider();

        // Hook GatewayClient.InteractionCreate into InteractiveService.
        // See CreateInteractiveService for why this is done via reflection.
        var gatewayClient = provider.GetRequiredService<GatewayClient>();
        var interactiveService = provider.GetRequiredService<InteractiveService>();
        var interactionCreatedMethod = typeof(InteractiveService)
            .GetMethod("InteractionCreated", BindingFlags.NonPublic | BindingFlags.Instance)!;
        gatewayClient.InteractionCreate += interaction =>
            (ValueTask)interactionCreatedMethod.Invoke(interactiveService, [gatewayClient, interaction])!;

        provider.GetRequiredService<MessageHandler>();
        provider.GetRequiredService<InteractionHandler>();
        provider.GetRequiredService<UserUpdateHandler>();
        provider.GetRequiredService<GuildMemberUpdateHandler>();
        provider.GetRequiredService<GuildMemberRemoveHandler>();
        provider.GetRequiredService<ClientJoinedGuildHandler>();
        provider.GetRequiredService<ClientLeftGuildHandler>();

        await provider.GetRequiredService<BotService>().StartAsync();

        using var server = new BackgroundJobServer();

        await Task.Delay(-1);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.Configure<BotEnvConfig>(Configuration);

        // Read the discord token to create the client
        var discordToken = Configuration["Discord:Token"]
            ?? throw new InvalidOperationException("Discord:Token environment variable not set.");

        var discordClient = new GatewayClient(
            new BotToken(discordToken),
            new GatewayClientConfiguration
            {
                // TODO: Add GatewayIntents.MessageContent when we have permission from Discord
                Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages |
                          GatewayIntents.GuildMessageReactions | GatewayIntents.GuildUsers |
                          GatewayIntents.DirectMessages | GatewayIntents.DirectMessageReactions
            });

        services
            .AddSingleton(discordClient)
            .AddSingleton<IDiscordUserResolver, DiscordUserResolver>()
            .AddSingleton<IDmSender, DmSender>()
            .AddSingleton(new CommandService<CommandContext>())
            .AddSingleton(new ApplicationCommandService<ApplicationCommandContext, AutocompleteInteractionContext>(
                ApplicationCommandServiceConfiguration<ApplicationCommandContext>.Default with
                {
                    EnumTypeReader = new LenientEnumSlashCommandTypeReader<ApplicationCommandContext>()
                }))
            .AddSingleton<ComponentInteractionService<ComponentInteractionContext>>()
            .AddSingleton<ComponentInteractionService<ModalInteractionContext>>()
            .AddSingleton<UserService>()
            .AddSingleton<InteractiveService>(CreateInteractiveService)
            .AddSingleton<GuildService>()
            .AddSingleton<GuildMemberLookupService>()
            .AddSingleton<IPrefixService, PrefixService>()
            .AddSingleton<SupporterService>()
            .AddSingleton<BackgroundService>()
            .AddSingleton<MessageHandler>()
            .AddSingleton<ClientJoinedGuildHandler>()
            .AddSingleton<ClientLeftGuildHandler>()
            .AddSingleton<GuildMemberRemoveHandler>()
            .AddSingleton<GuildMemberUpdateHandler>()
            .AddSingleton<UserUpdateHandler>()
            .AddSingleton<BotService>()
            .AddSingleton<StylingUtilities>()
            .AddSingleton<GameService>()
            .AddSingleton<GameCommand>()
            .AddSingleton<GameCommands>()
            .AddSingleton<UrbanCommand>()
            .AddSingleton<AvatarCommand>()
            .AddSingleton<BannerCommand>()
            .AddSingleton<ServerCommand>()
            .AddSingleton<UserCommand>()
            .AddSingleton<ProfileEditCommand>()
            .AddSingleton<BentoCommand>()
            .AddSingleton<BentoService>()
            .AddSingleton<WeatherService>()
            .AddSingleton<WeatherCommand>()
            .AddSingleton<LastFmCommands>()
            .AddSingleton<LastFmService>()
            .AddSingleton<LastFmCommand>()
            .AddSingleton<TagCommands>()
            .AddSingleton<TagsCommand>()
            .AddSingleton<TagService>()
            .AddSingleton<ReminderCommands>()
            .AddSingleton<ReminderService>()
            .AddSingleton<ReminderCommand>()
            .AddSingleton<ProfileCommands>()
            .AddSingleton<ProfileService>()
            .AddSingleton<SpotifyApiService>()
            .AddSingleton<ImageCommands>()
            .AddSingleton<ToolsCommand>()
            .AddSingleton<LeaderboardService>()
            .AddSingleton<LeaderboardCommand>()
            .AddSingleton<GuildSettingService>()
            .AddSingleton<UserSettingService>()
            .AddSingleton<SettingsCommand>()
            .AddSingleton<HtmlSanitizer>()
            .AddSingleton(Configuration);

        services.AddSingleton<InteractionHandler>();

        services.AddHttpClient<BotListService>();
        services.AddHttpClient<StylingUtilities>();
        services.AddHttpClient<UrbanDictionaryService>();
        services.AddHttpClient<WeatherApiService>();
        services.AddHttpClient<LastFmApiService>();
        services.AddHttpClient<SushiiImageServerService>();
        services.AddHttpClient<BentoMediaServerService>(client =>
        {
            // Resolve calls are fast; proxy calls stream video so we allow more time
            client.Timeout = TimeSpan.FromSeconds(90);
        });
        services.AddSingleton<MediaCommand>()
            .AddSingleton<MediaRateLimitService>();

        services.AddHttpClient<SpotifyApiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHealthChecks();

        services.AddDbContextFactory<BotDbContext>(b =>
            b.UseNpgsql(Configuration["PostgreSQL:ConnectionString"]).ConfigureWarnings(builder =>
                builder.Log(RelationalEventId.PendingModelChangesWarning)));

        // Configure shared distributed cache (Valkey/Redis) for cross-process caching. Fail if not configured.
        var distributedCacheConnection = Configuration["Valkey:ConnectionString"]
            ?? Configuration["Redis:ConnectionString"]
            ?? Configuration["RedisConnectionString"];
        if (!string.IsNullOrWhiteSpace(distributedCacheConnection))
        {
            services.AddStackExchangeRedisCache(opts =>
            {
                opts.Configuration = distributedCacheConnection;
                opts.InstanceName = "dotbento:";
            });
        }
        else
        {
            throw new InvalidOperationException("Valkey/Redis connection string is not configured. Set Valkey:ConnectionString (env: VALKEY__CONNECTIONSTRING) or Redis:ConnectionString (env: REDIS__CONNECTIONSTRING) or RedisConnectionString (env: REDISCONNECTIONSTRING) to enable the shared cache.");
        }

        // Keep IMemoryCache for local per-process caching used elsewhere
        services.AddMemoryCache();
    }

    /// <summary>
    /// Creates an <see cref="InteractiveService"/> without its constructor to avoid the
    /// <see cref="NetCord.Gateway.ShardedGatewayClient"/> dependency.
    /// Fergun.Interactive.NetCord 0.1.1 only exposes constructors that accept ShardedGatewayClient,
    /// but dotBento uses a single-shard GatewayClient. We bypass the constructor via
    /// RuntimeHelpers.GetUninitializedObject and wire up the GatewayClient.InteractionCreate event
    /// separately in RunAsync.
    /// </summary>
    private static InteractiveService CreateInteractiveService(IServiceProvider _)
    {
        var service = (InteractiveService)RuntimeHelpers.GetUninitializedObject(typeof(InteractiveService));
        var t = typeof(InteractiveService);

        // _client: not needed — we subscribe GatewayClient.InteractionCreate manually in RunAsync
        t.GetField("_client", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(service, null!);

        // _logger: use NullLogger since Serilog is wired statically, not via ILogger<T>
        t.GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(service, NullLogger<InteractiveService>.Instance);

        // _callbacks and _filteredCallbacks: standard ConcurrentDictionary instances
        var callbacksField = t.GetField("_callbacks", BindingFlags.NonPublic | BindingFlags.Instance)!;
        callbacksField.SetValue(service, Activator.CreateInstance(callbacksField.FieldType)!);

        var filteredField = t.GetField("_filteredCallbacks", BindingFlags.NonPublic | BindingFlags.Instance)!;
        filteredField.SetValue(service, Activator.CreateInstance(filteredField.FieldType)!);

        // _options: default options
        t.GetField("_options", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(service, new InteractiveServiceOptions());

        return service;
    }

    private static void AppUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception exception) return;
        UnhandledExceptions(exception);

        if (e.IsTerminating)
        {
            Log.CloseAndFlush();
        }
    }

    private static void UnhandledExceptions(Exception e)
    {
        Log.Logger.Error(e, "dotbento crashed");
    }
}
