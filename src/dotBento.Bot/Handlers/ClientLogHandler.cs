using Discord;
using Discord.WebSocket;
using Serilog;

namespace dotBento.Bot.Handlers;

public class ClientLogHandler
{
    private readonly DiscordSocketClient _client;

    public ClientLogHandler(DiscordSocketClient client)
    {
        _client = client;
        _client.Log += LogEvent;
    }

    private static Task LogEvent(LogMessage logMessage)
    {
        Task.Run(() =>
        {
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

        });
        return Task.CompletedTask;
    }
}