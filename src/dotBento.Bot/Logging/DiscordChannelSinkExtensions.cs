using Discord.WebSocket;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace dotBento.Bot.Logging;

/// <summary>
/// Extension methods for configuring the Discord channel sink in Serilog.
/// </summary>
public static class DiscordChannelSinkExtensions
{
    private static DiscordChannelSink? _instance;
    private static readonly object Lock = new();

    /// <summary>
    /// Adds a Discord channel sink to the Serilog configuration.
    /// The sink queues log events until <see cref="ActivateDiscordChannelSink"/> is called.
    /// </summary>
    /// <param name="config">The sink configuration.</param>
    /// <param name="channelId">The Discord channel ID to send logs to.</param>
    /// <param name="minimumLevel">The minimum log level to send to Discord. Defaults to Warning.</param>
    /// <returns>The logger configuration for method chaining.</returns>
    public static LoggerConfiguration DiscordChannel(
        this LoggerSinkConfiguration config,
        ulong channelId,
        LogEventLevel minimumLevel = LogEventLevel.Warning)
    {
        lock (Lock)
        {
            _instance = new DiscordChannelSink(channelId, minimumLevel);
        }

        return config.Sink(_instance);
    }

    /// <summary>
    /// Activates the Discord channel sink with the given client.
    /// Call this when the Discord client is ready.
    /// </summary>
    /// <param name="client">The Discord socket client.</param>
    public static void ActivateDiscordChannelSink(DiscordSocketClient client)
    {
        lock (Lock)
        {
            _instance?.Activate(client);
        }
    }
}
