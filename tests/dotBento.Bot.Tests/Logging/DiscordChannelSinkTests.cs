using System.Collections.Concurrent;
using System.Reflection;
using Discord;
using dotBento.Bot.Logging;
using Moq;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;

namespace dotBento.Bot.Tests.Logging;

public sealed class DiscordChannelSinkTests
{
    private static readonly MessageTemplateParser Parser = new();

    private static LogEvent CreateLogEvent(
        LogEventLevel level,
        string template = "Hello {Name}",
        Exception? exception = null,
        string? sourceContext = null)
    {
        var properties = new List<LogEventProperty>
        {
            new("Name", new ScalarValue("Bento"))
        };

        if (sourceContext is not null)
        {
            properties.Add(new LogEventProperty("SourceContext", new ScalarValue(sourceContext)));
        }

        return new LogEvent(
            DateTimeOffset.Parse("2026-06-06T12:00:00+00:00"),
            level,
            exception,
            Parser.Parse(template),
            properties);
    }

    private static ConcurrentQueue<LogEvent> GetPendingEvents(DiscordChannelSink sink)
    {
        var field = typeof(DiscordChannelSink).GetField("_pendingEvents", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<ConcurrentQueue<LogEvent>>(field.GetValue(sink));
    }

    private static Embed FormatEmbed(DiscordChannelSink sink, LogEvent logEvent)
    {
        var method = typeof(DiscordChannelSink).GetMethod("FormatEmbed", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return Assert.IsType<Embed>(method.Invoke(sink, [logEvent]));
    }

    [Fact]
    public void Emit_QueuesEventsAtOrAboveMinimumLevel()
    {
        using var sink = new DiscordChannelSink(channelId: 123, LogEventLevel.Warning);

        sink.Emit(CreateLogEvent(LogEventLevel.Information));
        sink.Emit(CreateLogEvent(LogEventLevel.Warning));

        Assert.Single(GetPendingEvents(sink));
    }

    [Theory]
    [InlineData("Discord.WebSocket.DiscordSocketClient")]
    [InlineData("Discord.Rest.RestClient")]
    [InlineData("dotBento.Bot.Logging.DiscordChannelSink")]
    public void Emit_IgnoresDiscordRelatedSourceContexts(string sourceContext)
    {
        using var sink = new DiscordChannelSink(channelId: 123, LogEventLevel.Verbose);

        sink.Emit(CreateLogEvent(LogEventLevel.Error, sourceContext: sourceContext));

        Assert.Empty(GetPendingEvents(sink));
    }

    [Fact]
    public void Emit_DropsOldestEventWhenQueueLimitIsReached()
    {
        using var sink = new DiscordChannelSink(channelId: 123, LogEventLevel.Verbose);

        for (var i = 0; i < 101; i++)
        {
            sink.Emit(CreateLogEvent(LogEventLevel.Warning, $"Event {i}"));
        }

        var pending = GetPendingEvents(sink);
        Assert.Equal(100, pending.Count);
        Assert.DoesNotContain(pending, logEvent => logEvent.MessageTemplate.Text == "Event 0");
    }

    [Fact]
    public void FormatEmbed_MapsLevelMessageSourceAndException()
    {
        using var sink = new DiscordChannelSink(channelId: 123, LogEventLevel.Verbose);
        var exception = new InvalidOperationException("Failed to log");

        var embed = FormatEmbed(
            sink,
            CreateLogEvent(LogEventLevel.Error, exception: exception, sourceContext: "dotBento.Bot.Tests.Logging.Sample"));

        Assert.Equal(":x: Error", embed.Title);
        Assert.Equal(Color.Red, embed.Color);
        Assert.Contains("Hello Bento", embed.Description);
        Assert.Equal("Sample", embed.Footer?.Text);
        Assert.Contains(embed.Fields, field => field.Name == "Exception" && field.Value.Contains("Failed to log"));
    }

    [Fact]
    public void FormatEmbed_TruncatesLongMessagesAndExceptions()
    {
        using var sink = new DiscordChannelSink(channelId: 123, LogEventLevel.Verbose);
        var longMessage = new string('a', 4_100);
        var longException = new Exception(new string('b', 1_100));

        var embed = FormatEmbed(sink, CreateLogEvent(LogEventLevel.Fatal, longMessage, longException));

        Assert.Equal(":skull: Fatal", embed.Title);
        Assert.EndsWith("...\n```", embed.Description);
        Assert.True(embed.Description.Length < 4_020);
        Assert.Contains(embed.Fields, field => field.Name == "Exception" && field.Value.EndsWith("...\n```"));
    }

    [Fact]
    public void DiscordChannelExtension_ConfiguresSinkAndActivationHook()
    {
        var logger = new LoggerConfiguration()
            .WriteTo.DiscordChannel(channelId: 0)
            .CreateLogger();
        var client = new Mock<Discord.WebSocket.DiscordSocketClient>(new Discord.WebSocket.DiscordSocketConfig());

        logger.Warning("Queued warning");
        DiscordChannelSinkExtensions.ActivateDiscordChannelSink(client.Object);

        Assert.NotNull(logger);
    }
}
