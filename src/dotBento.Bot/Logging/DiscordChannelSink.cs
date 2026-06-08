using System.Collections.Concurrent;
using NetCord.Gateway;
using NetCord.Rest;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace dotBento.Bot.Logging;

/// <summary>
/// A Serilog sink that sends log events to a Discord text channel.
/// Events are queued until the Discord client is ready and the sink is activated.
/// </summary>
public sealed class DiscordChannelSink : ILogEventSink, IDisposable
{
    private readonly ulong _channelId;
    private readonly LogEventLevel _minimumLevel;
    private readonly ConcurrentQueue<LogEvent> _pendingEvents = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly Timer _flushTimer;
    private readonly MessageTemplateTextFormatter _formatter;

    private GatewayClient? _client;
    private bool _isActivated;
    private bool _isDisabled;
    private bool _isDisposed;

    private const int MaxEmbedsPerFlush = 5;
    private const int FlushIntervalMs = 2000;
    private const int MaxPendingEvents = 100;

    public DiscordChannelSink(ulong channelId, LogEventLevel minimumLevel)
    {
        _channelId = channelId;
        _minimumLevel = minimumLevel;
        _formatter = new MessageTemplateTextFormatter("{Message:lj}");

        _flushTimer = new Timer(
            _ => _ = FlushAsync(),
            null,
            Timeout.Infinite,
            Timeout.Infinite);
    }

    /// <summary>
    /// Activates the sink with the Discord client. Called when the client is ready.
    /// </summary>
    public void Activate(GatewayClient client)
    {
        if (_isActivated || _isDisabled || _channelId == 0)
            return;

        _client = client;
        _isActivated = true;

        // Start the flush timer
        _flushTimer.Change(FlushIntervalMs, FlushIntervalMs);

        // Trigger initial flush for queued events
        _ = FlushAsync();
    }

    /// <summary>
    /// Emits a log event to the sink. Events are queued until the sink is activated.
    /// </summary>
    public void Emit(LogEvent logEvent)
    {
        if (_isDisabled || _isDisposed)
            return;

        if (logEvent.Level < _minimumLevel)
            return;

        // Prevent infinite loops by filtering NetCord-related logs
        if (IsNetCordRelatedLog(logEvent))
            return;

        // Limit queue size to prevent memory issues
        if (_pendingEvents.Count >= MaxPendingEvents)
        {
            // Drop oldest events if queue is full
            _pendingEvents.TryDequeue(out _);
        }

        _pendingEvents.Enqueue(logEvent);

        // If activated, trigger an immediate flush for high-priority events
        if (_isActivated && logEvent.Level >= LogEventLevel.Error)
        {
            _ = FlushAsync();
        }
    }

    private static bool IsNetCordRelatedLog(LogEvent logEvent)
    {
        // Check source context for NetCord-related namespaces to avoid infinite loops
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
        {
            var source = sourceContext.ToString().Trim('"');

            if (source.StartsWith("NetCord.", StringComparison.OrdinalIgnoreCase) ||
                source.StartsWith("NetCord.Gateway", StringComparison.OrdinalIgnoreCase) ||
                source.StartsWith("NetCord.Rest", StringComparison.OrdinalIgnoreCase) ||
                source.Contains("DiscordChannelSink", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task FlushAsync()
    {
        if (!_isActivated || _isDisabled || _client == null || _pendingEvents.IsEmpty)
            return;

        if (!await _sendLock.WaitAsync(0))
            return;

        try
        {
            var embedList = new List<EmbedProperties>();
            var eventCount = 0;

            while (_pendingEvents.TryDequeue(out var logEvent) && eventCount < MaxEmbedsPerFlush)
            {
                embedList.Add(FormatEmbed(logEvent));
                eventCount++;
            }

            if (embedList.Count > 0)
            {
                await _client.Rest.SendMessageAsync(_channelId, new MessageProperties { Embeds = embedList });
            }
        }
        catch
        {
            // If we fail to send, disable the sink to prevent further issues
            _isDisabled = true;
            _flushTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private EmbedProperties FormatEmbed(LogEvent logEvent)
    {
        var color = logEvent.Level switch
        {
            LogEventLevel.Fatal => new NetCord.Color(0x8B0000),
            LogEventLevel.Error => new NetCord.Color(0xFF0000),
            LogEventLevel.Warning => new NetCord.Color(0xFFD700),
            LogEventLevel.Information => new NetCord.Color(0x0000FF),
            LogEventLevel.Debug => new NetCord.Color(0xD3D3D3),
            LogEventLevel.Verbose => new NetCord.Color(0xA9A9A9),
            _ => new NetCord.Color(0x000000)
        };

        var levelEmoji = logEvent.Level switch
        {
            LogEventLevel.Fatal => ":skull:",
            LogEventLevel.Error => ":x:",
            LogEventLevel.Warning => ":warning:",
            LogEventLevel.Information => ":information_source:",
            LogEventLevel.Debug => ":beetle:",
            LogEventLevel.Verbose => ":mag:",
            _ => ":grey_question:"
        };

        var message = RenderMessage(logEvent);

        // Truncate message if too long for Discord embed
        const int maxMessageLength = 4000;
        if (message.Length > maxMessageLength)
        {
            message = string.Concat(message.AsSpan(0, maxMessageLength - 3), "...");
        }

        var embed = new EmbedProperties()
            .WithColor(color)
            .WithTitle($"{levelEmoji} {logEvent.Level}")
            .WithDescription($"```\n{message}\n```")
            .WithTimestamp(logEvent.Timestamp);

        // Add source context if available
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
        {
            var source = sourceContext.ToString().Trim('"');
            // Shorten long namespace paths
            var shortSource = source.Contains('.')
                ? source[(source.LastIndexOf('.') + 1)..]
                : source;
            embed.WithFooter(new EmbedFooterProperties().WithText(shortSource));
        }

        // Add exception details if present
        if (logEvent.Exception != null)
        {
            var exceptionText = logEvent.Exception.ToString();
            const int maxExceptionLength = 1000;
            if (exceptionText.Length > maxExceptionLength)
            {
                exceptionText = string.Concat(exceptionText.AsSpan(0, maxExceptionLength - 3), "...");
            }
            embed.WithFields([new EmbedFieldProperties().WithName("Exception").WithValue($"```\n{exceptionText}\n```")]);
        }

        return embed;
    }

    private string RenderMessage(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        return writer.ToString();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _flushTimer.Dispose();
        _sendLock.Dispose();
    }
}
