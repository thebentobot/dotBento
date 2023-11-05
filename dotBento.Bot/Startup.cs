using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Handlers;
using dotBento.Bot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;


var builder = new HostBuilder();

var directory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
var configPath = Path.Combine(directory, "../../../appsettings.json");

builder.ConfigureAppConfiguration(options =>
    options.AddJsonFile(configPath)
        .AddEnvironmentVariables());

var loggerConfig = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File($"logs/log-{DateTime.Now:dd.MM.yy_HH.mm}.log")
    .CreateLogger();

builder.ConfigureServices((host, services) =>
{
    services.AddLogging(options => options.AddSerilog(loggerConfig, dispose: true));

    services.AddSingleton(new DiscordSocketClient(
        new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All,
            FormatUsersInBidirectionalUnicode = false,
            AlwaysDownloadUsers = true,
            LogGatewayIntentWarnings = false,
            LogLevel = LogSeverity.Info
        }));

    services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(), new InteractionServiceConfig()
    {
        LogLevel = LogSeverity.Info
    }));

    services.AddSingleton<InteractionHandler>();

    services.AddHostedService<BotService>();
});

var app = builder.Build();

await app.RunAsync();