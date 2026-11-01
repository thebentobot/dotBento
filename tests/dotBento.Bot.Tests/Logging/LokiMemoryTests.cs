using System.Net;
using System.Runtime.CompilerServices;
using dotBento.Domain;
using Moq;
using Moq.Protected;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.Grafana.Loki;

namespace dotBento.Bot.Tests.Logging;

public sealed class LokiMemoryTests
{
    [Fact]
    public async Task UnavailableLoki_BufferRetainsAtMostConfiguredQueueCapacity()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage _, CancellationToken token) =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(token);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            });
        using var http = new HttpClient(handler.Object);
        await using var logger = new LoggerConfiguration().WriteTo.GrafanaLoki(
            "http://loki.test", httpClient: http,
            queueLimit: RemoteLoggingConfig.LokiQueueLimit,
            batchSizeLimit: 100,
            retryTimeLimit: TimeSpan.FromMilliseconds(1)).CreateLogger();
        logger.Information("Start blocked delivery");
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            var references = FillQueue(logger, RemoteLoggingConfig.LokiQueueLimit + 500);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var retained = references.Count(r => r.TryGetTarget(out _));
            Assert.InRange(retained, 1, RemoteLoggingConfig.LokiQueueLimit);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<WeakReference<LogEvent>> FillQueue(ILogger logger, int count)
    {
        var template = new MessageTemplateParser().Parse("Queued event");
        var references = new List<WeakReference<LogEvent>>(count);
        for (var i = 0; i < count; i++)
        {
            var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information, null, template, []);
            references.Add(new WeakReference<LogEvent>(evt));
            logger.Write(evt);
        }
        return references;
    }
}
