using Discord;
using Discord.WebSocket;
using dotBento.Domain;
using Serilog;

namespace dotBento.Bot.Handlers;

public sealed class ClientLogHandler : IDisposable
{
    private readonly DiscordSocketClient _client;

    public ClientLogHandler(DiscordSocketClient client)
    {
        _client = client;
        _client.Log += LogEvent;
    }

    /// <summary>
    /// Discord.Net raises "Rate limit triggered: ..." at warning level when a request is answered with
    /// HTTP 429, and "Preemptive Rate limit triggered: ..." at verbose level when it throttles itself
    /// before sending. Only the first is a real rate limit, so the preemptive message is ignored.
    /// </summary>
    internal static bool IsRateLimitResponse(LogMessage logMessage) =>
        logMessage.Message?.Contains("Rate limit triggered", StringComparison.OrdinalIgnoreCase) == true &&
        logMessage.Message.Contains("Preemptive", StringComparison.OrdinalIgnoreCase) == false;
    private static Task LogEvent(LogMessage logMessage)
    {
        if (IsRateLimitResponse(logMessage))
        {
            Statistics.DiscordRateLimits.Inc();
        }

        switch (logMessage.Severity)
        {
            case LogSeverity.Critical:
                Log.Fatal(logMessage.Exception, "{LogMessageSource} | {LogMessage}", logMessage.Source, logMessage.Message);
                break;
            case LogSeverity.Error:
                Log.Error(logMessage.Exception, "{LogMessageSource} | {LogMessage}", logMessage.Source, logMessage.Message);
                break;
            case LogSeverity.Warning:
                Log.Warning(logMessage.Exception, "{LogMessageSource} | {LogMessage}", logMessage.Source, logMessage.Message);
                break;
            case LogSeverity.Info:
                Log.Information(logMessage.Exception, "{LogMessageSource} | {LogMessage}", logMessage.Source, logMessage.Message);
                break;
            case LogSeverity.Verbose:
                Log.Verbose(logMessage.Exception, "{LogMessageSource} | {LogMessage}", logMessage.Source, logMessage.Message);
                break;
            case LogSeverity.Debug:
                Log.Debug(logMessage.Exception, "{LogMessageSource} | {LogMessage}", logMessage.Source, logMessage.Message);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _client.Log -= LogEvent;
    }
}
