using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
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

    private ITextChannel? _channel;
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
    public void Activate(DiscordSocketClient client)
    {
        Console.WriteLine($"[DiscordChannelSink] Activate called. ChannelId={_channelId}, IsActivated={_isActivated}, IsDisabled={_isDisabled}");

        if (_isActivated || _isDisabled || _channelId == 0)
        {
            Console.WriteLine("[DiscordChannelSink] Skipping activation (already activated, disabled, or no channel configured)");
            return;
        }

        try
        {
            var channel = client.GetChannel(_channelId);
            Console.WriteLine($"[DiscordChannelSink] GetChannel returned: {channel?.GetType().Name ?? "null"}");

            if (channel is not ITextChannel textChannel)
            {
                Console.WriteLine($"[DiscordChannelSink] Channel {_channelId} is not a text channel or not accessible. Sink disabled.");
                _isDisabled = true;
                return;
            }

            _channel = textChannel;
            _isActivated = true;

            // Start the flush timer
            _flushTimer.Change(FlushIntervalMs, FlushIntervalMs);

            Console.WriteLine($"[DiscordChannelSink] Activated successfully! Channel: #{textChannel.Name} in {textChannel.Guild.Name}. Queued events: {_pendingEvents.Count}");

            // Trigger initial flush for queued events
            _ = FlushAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DiscordChannelSink] Failed to activate: {ex.Message}. Sink disabled.");
            _isDisabled = true;
        }
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

        // Prevent infinite loops by filtering Discord-related logs
        if (IsDiscordRelatedLog(logEvent))
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

    private static bool IsDiscordRelatedLog(LogEvent logEvent)
    {
        // Check source context for Discord-related namespaces
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
        {
            var source = sourceContext.ToString().Trim('"');
            if (source.Contains("Discord", StringComparison.OrdinalIgnoreCase) ||
                source.Contains("DiscordChannelSink", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Also check the message itself
        var message = RenderMessage(logEvent);
        return message.Contains("Discord", StringComparison.OrdinalIgnoreCase) &&
               (message.Contains("Gateway", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("WebSocket", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("REST", StringComparison.OrdinalIgnoreCase));
    }

    private async Task FlushAsync()
    {
        if (!_isActivated || _isDisabled || _channel == null || _pendingEvents.IsEmpty)
            return;

        if (!await _sendLock.WaitAsync(0))
            return;

        try
        {
            var embeds = new List<Embed>();
            var eventCount = 0;

            while (_pendingEvents.TryDequeue(out var logEvent) && eventCount < MaxEmbedsPerFlush)
            {
                embeds.Add(FormatEmbed(logEvent));
                eventCount++;
            }

            if (embeds.Count > 0)
            {
                Console.WriteLine($"[DiscordChannelSink] Sending {embeds.Count} log(s) to Discord channel...");
                await _channel.SendMessageAsync(embeds: embeds.ToArray());
                Console.WriteLine($"[DiscordChannelSink] Successfully sent {embeds.Count} log(s)");
            }
        }
        catch (Exception ex)
        {
            // If we fail to send, disable the sink to prevent further issues
            Console.WriteLine($"[DiscordChannelSink] Failed to send logs: {ex.Message}. Sink disabled.");
            _isDisabled = true;
            _flushTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private Embed FormatEmbed(LogEvent logEvent)
    {
        var color = logEvent.Level switch
        {
            LogEventLevel.Fatal => Color.DarkRed,
            LogEventLevel.Error => Color.Red,
            LogEventLevel.Warning => Color.Gold,
            LogEventLevel.Information => Color.Blue,
            LogEventLevel.Debug => Color.LightGrey,
            LogEventLevel.Verbose => Color.DarkGrey,
            _ => Color.Default
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

        var builder = new EmbedBuilder()
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
            builder.WithFooter(shortSource);
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
            builder.AddField("Exception", $"```\n{exceptionText}\n```");
        }

        return builder.Build();
    }

    private static string RenderMessage(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        var formatter = new MessageTemplateTextFormatter("{Message:lj}");
        formatter.Format(logEvent, writer);
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
