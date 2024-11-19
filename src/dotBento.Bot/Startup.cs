using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Configurations;
using dotBento.Bot.Handlers;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Discord;
using BackgroundService = dotBento.Bot.Services.BackgroundService;
using RunMode = Discord.Commands.RunMode;

namespace dotBento.Bot;

public sealed class Startup
{
    private IConfiguration Configuration { get; }
    
    public Startup()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "configs", "config.json"), true)
            .AddEnvironmentVariables();
        Configuration = configBuilder.Build();

        Configuration.Bind(ConfigData.Data);
    }
    
    public static async Task RunAsync(string[] args)
    {
        var startup = new Startup();

        await startup.RunAsync();
    }
    
    private async Task RunAsync()
    {
        // ReSharper disable once RedundantAssignment
        var consoleLevel = LogEventLevel.Warning;
        // ReSharper disable once RedundantAssignment
        var logLevel = LogEventLevel.Information;
#if DEBUG
        consoleLevel = LogEventLevel.Verbose;
        logLevel = LogEventLevel.Information;
#endif

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(consoleLevel)
            .MinimumLevel.Is(logLevel)
            .Enrich.WithProperty("Environment", !string.IsNullOrEmpty(Configuration.GetSection("Environment").Value) ? Configuration.GetSection("Environment").Value : "unknown")
            .Enrich.WithExceptionDetails()
            .WriteTo.Discord(Convert.ToUInt64(Configuration["Discord:LogWebhookId"]), Configuration["Discord:LogWebhookToken"])
            //.WriteTo.File($"logs/log-{DateTime.Now:dd.MM.yy_HH.mm}.log")
            .CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += AppUnhandledException;

        Log.Information("dotBento is starting up...");

        var services = new ServiceCollection();
        ConfigureServices(services);
        
        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<MessageHandler>();
        provider.GetRequiredService<InteractionHandler>();
        provider.GetRequiredService<ClientLogHandler>();
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

        var discordClient = new DiscordSocketClient(new DiscordSocketConfig
        {
            // Add GatewayIntents.MessageContent when we have permission from Discord
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages |
                             GatewayIntents.GuildMessageReactions | GatewayIntents.GuildMembers |
                             GatewayIntents.DirectMessages | GatewayIntents.DirectMessageReactions,
            FormatUsersInBidirectionalUnicode = false,
            AlwaysDownloadUsers = true,
            LogGatewayIntentWarnings = true,
            LogLevel = LogSeverity.Info,
            MessageCacheSize = 0,
        });

        services
            .AddSingleton(discordClient)
            .AddSingleton(new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                DefaultRunMode = RunMode.Async
            }))
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(),
                new InteractionServiceConfig()
                {
                    LogLevel = LogSeverity.Info,
                    DefaultRunMode = Discord.Interactions.RunMode.Async
                }))
            .AddSingleton<UserService>()
            .AddSingleton<InteractiveService>()
            .AddSingleton<GuildService>()
            .AddSingleton<IPrefixService, PrefixService>()
            .AddSingleton<SupporterService>()
            .AddSingleton<BackgroundService>()
            .AddSingleton<MessageHandler>()
            .AddSingleton<ClientLogHandler>()
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
            .AddSingleton<HtmlSanitizer>()
            .AddSingleton(Configuration);

        services.AddSingleton<InteractionHandler>();
        
        services.AddHttpClient<StylingUtilities>();
        services.AddHttpClient<UrbanDictionaryService>();
        services.AddHttpClient<WeatherApiService>();
        services.AddHttpClient<LastFmApiService>();
        services.AddHttpClient<SushiiImageServerService>();
        
        services.AddHttpClient<SpotifyApiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHealthChecks();
        
        services.AddDbContextFactory<BotDbContext>(b => 
            b.UseNpgsql(Configuration["PostgreSQL:ConnectionString"]));
        
        services.AddMemoryCache();
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