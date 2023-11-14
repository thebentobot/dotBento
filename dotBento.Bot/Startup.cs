using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Factories;
using dotBento.Bot.Handlers;
using dotBento.Bot.Services;
using dotBento.Domain.Interfaces;
using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.Discord;

namespace dotBento.Bot;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var builder = new HostBuilder();

        var directory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
        var configPath = Path.Combine(directory ?? throw new InvalidOperationException(), "../../../appsettings.json");

        builder.ConfigureAppConfiguration(options =>
            options.AddJsonFile(configPath)
                .AddEnvironmentVariables());

        builder.ConfigureServices((hostContext, services) =>
        {
            var configuration = hostContext.Configuration;

            var webhookIdString = configuration["Discord:LogWebhookId"] ??
                                  throw new InvalidOperationException("LogWebhookId environment variable are not set.");
            var webhookToken = configuration["Discord:LogWebhookToken"] ??
                               throw new InvalidOperationException("LogWebhookToken environment variable are not set.");
            
            AppDomain.CurrentDomain.UnhandledException += AppUnhandledException;

            if (!ulong.TryParse(webhookIdString, out var webhookId))
            {
                throw new FormatException("LogWebhookId is not a valid ulong value.");
            }

            var loggerConfig = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.Discord(webhookId, webhookToken)
                //.WriteTo.File($"logs/log-{DateTime.Now:dd.MM.yy_HH.mm}.log")
                .CreateLogger();
    
            services.AddLogging(options => options.AddSerilog(loggerConfig, dispose: true));
    
            services.AddDbContextFactory<BotDbContext>(b =>
                b.UseNpgsql(configuration["Database:ConnectionString"]));

            services.AddSingleton(new DiscordSocketClient(
                new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages |
                                     GatewayIntents.GuildMessageReactions | GatewayIntents.GuildMembers |
                                     GatewayIntents.DirectMessages | GatewayIntents.DirectMessageReactions,
                    FormatUsersInBidirectionalUnicode = false,
                    AlwaysDownloadUsers = true,
                    LogGatewayIntentWarnings = true,
                    LogLevel = LogSeverity.Info,
                    MessageCacheSize = 0,
                }));

            services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(),
                new InteractionServiceConfig()
                {
                    LogLevel = LogSeverity.Info
                }));

            services.AddSingleton<InteractionHandler>();
            services.AddSingleton<ClientLogHandler>();

            services.AddSingleton<IBotDbContextFactory, BotDbContextFactory>();

            services.AddHostedService<BotService>();
    
            services.AddHealthChecks();

            services.AddMemoryCache();
        });

        var app = builder.Build();

        await app.RunAsync();
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