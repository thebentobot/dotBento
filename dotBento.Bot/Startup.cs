using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Factories;
using dotBento.Bot.Handlers;
using dotBento.Bot.Interfaces;
using dotBento.Bot.Models;
using dotBento.Bot.Services;
using dotBento.Domain.Interfaces;
using dotBento.EntityFramework.Context;
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

public class Startup
{
    private IConfiguration Configuration { get; }
    public Startup(string[] args)
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../../../configs"))
            .AddJsonFile("config.json")
            .AddEnvironmentVariables();

        Configuration = configBuilder.Build();
    }
    
    public static async Task RunAsync(string[] args)
    {
        var startup = new Startup(args);

        await startup.RunAsync();
    }
    
    private async Task RunAsync()
    {
        var consoleLevel = LogEventLevel.Warning;
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
                DefaultRunMode = RunMode.Async,
            }))
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(),
                new InteractionServiceConfig()
                {
                    LogLevel = LogSeverity.Info
                }))
            .AddSingleton<InteractionHandler>()
            .AddSingleton<UserService>()
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
            .AddSingleton<IBotDbContextFactory, BotDbContextFactory>()
            .AddSingleton(Configuration);

        services.AddSingleton<InteractionHandler>();

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