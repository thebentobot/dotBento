using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Handlers;
using dotBento.Bot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.Discord;

var builder = new HostBuilder();

var directory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
var configPath = Path.Combine(directory ?? throw new InvalidOperationException(), "../../../appsettings.json");

builder.ConfigureAppConfiguration(options =>
    options.AddJsonFile(configPath)
        .AddEnvironmentVariables());

builder.ConfigureServices((hostContext, services) =>
{
    var configuration = hostContext.Configuration;

    string webhookIdString = configuration["Discord:LogWebhookId"] ??
                             throw new InvalidOperationException("LogWebhookId environment variable are not set.");
    string webhookToken = configuration["Discord:LogWebhookToken"] ??
                          throw new InvalidOperationException("LogWebhookToken environment variable are not set.");

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
        }));

    services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(),
        new InteractionServiceConfig()
    {
        LogLevel = LogSeverity.Info
    }));

    services.AddSingleton<InteractionHandler>();

    services.AddHostedService<BotService>();
});

var app = builder.Build();

await app.RunAsync();