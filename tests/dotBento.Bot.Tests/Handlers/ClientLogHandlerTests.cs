using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using dotBento.Bot.Handlers;
using Moq;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace dotBento.Bot.Tests.Handlers;

public sealed class ClientLogHandlerTests
{
    private sealed class CapturingSink : ILogEventSink
    {
        public ConcurrentQueue<LogEvent> Events { get; } = new();

        public void Emit(LogEvent logEvent) => Events.Enqueue(logEvent);
    }

    private static async Task InvokeLogEvent(LogMessage logMessage)
    {
        var method = typeof(ClientLogHandler).GetMethod("LogEvent", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);
        var task = Assert.IsAssignableFrom<Task>(method.Invoke(null, [logMessage]));
        await task;
    }

    [Fact]
    public async Task CapturingSink_EnumerationRemainsStableWhileAnotherThreadLogs()
    {
        var sink = new CapturingSink();
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        logger.Information("Before enumeration");

        using var events = sink.Events.GetEnumerator();
        Assert.True(events.MoveNext());
        Assert.Equal("Before enumeration", events.Current.MessageTemplate.Text);

        await Task.Run(() => logger.Information("During enumeration"), TestContext.Current.CancellationToken);

        Assert.False(events.MoveNext());
        Assert.Collection(sink.Events,
            logEvent => Assert.Equal("Before enumeration", logEvent.MessageTemplate.Text),
            logEvent => Assert.Equal("During enumeration", logEvent.MessageTemplate.Text));
    }

    [Theory]
    [InlineData(LogSeverity.Critical, LogEventLevel.Fatal)]
    [InlineData(LogSeverity.Error, LogEventLevel.Error)]
    [InlineData(LogSeverity.Warning, LogEventLevel.Warning)]
    [InlineData(LogSeverity.Info, LogEventLevel.Information)]
    [InlineData(LogSeverity.Verbose, LogEventLevel.Verbose)]
    [InlineData(LogSeverity.Debug, LogEventLevel.Debug)]
    public async Task LogEvent_MapsDiscordSeverityToSerilogLevel(LogSeverity severity, LogEventLevel expectedLevel)
    {
        var sink = new CapturingSink();
        var previousLogger = Log.Logger;
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .CreateLogger();
        Log.Logger = logger;

        try
        {
            var exception = new InvalidOperationException("discord log failed");

            await InvokeLogEvent(new LogMessage(severity, "DiscordSource", "Discord message", exception));

            var logEvent = Assert.Single(sink.Events, logEvent => logEvent.MessageTemplate.Text == "{LogMessageSource} | {LogMessage}");
            Assert.Equal(expectedLevel, logEvent.Level);
            Assert.Same(exception, logEvent.Exception);
            Assert.Equal("\"DiscordSource\"", logEvent.Properties["LogMessageSource"].ToString());
            Assert.Equal("\"Discord message\"", logEvent.Properties["LogMessage"].ToString());
        }
        finally
        {
            Log.Logger = previousLogger;
        }
    }

    [Fact]
    public async Task LogEvent_ThrowsForUnknownSeverity()
    {
        var exception = await Assert.ThrowsAsync<System.Reflection.TargetInvocationException>(
            () => InvokeLogEvent(new LogMessage((LogSeverity)999, "DiscordSource", "Discord message")));

        Assert.IsType<ArgumentOutOfRangeException>(exception.InnerException);
    }

    [Fact]
    public void Dispose_CanUnsubscribeFromClientLogEvent()
    {
        var client = new Mock<DiscordSocketClient>(new DiscordSocketConfig());
        using var handler = new ClientLogHandler(client.Object);

        handler.Dispose();
    }
}
