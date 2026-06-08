using System.Net;
using dotBento.Infrastructure.Services.Api;
using Moq;
using Moq.Protected;

namespace dotBento.Infrastructure.Tests.Services.Api;

public sealed class StreamingApiServiceTests
{
    private sealed class TrackingStream(byte[] buffer) : MemoryStream(buffer)
    {
        public bool WasDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            WasDisposed = true;
            await base.DisposeAsync();
        }
    }

    private static HttpClient CreateHttpClient(HttpResponseMessage response)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return new HttpClient(handler.Object);
    }

    [Fact]
    public async Task SushiiImageStream_BuffersResponseAndDisposesHttpContent()
    {
        var stream = new TrackingStream([1, 2, 3]);
        using var httpClient = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stream)
        });
        var sut = new SushiiImageServerService(httpClient);

        var result = await sut.GetSushiiImage("https://images.example/render", "<html></html>", 100, 100);

        Assert.True(result.IsSuccess);
        Assert.True(stream.WasDisposed);
        Assert.IsType<MemoryStream>(result.Value);
        Assert.Equal(3, result.Value.Length);
        await result.Value.DisposeAsync();
    }

    [Fact]
    public async Task SushiiImageStream_DisposesResponseContentWhenRequestFails()
    {
        var stream = new TrackingStream([1, 2, 3]);
        using var httpClient = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StreamContent(stream)
        });
        var sut = new SushiiImageServerService(httpClient);

        var result = await sut.GetSushiiImage("https://images.example/render", "<html></html>", 100, 100);

        Assert.True(result.IsFailure);
        Assert.True(stream.WasDisposed);
    }

    [Fact]
    public async Task BentoProxyStream_DisposesResponseContentWhenReturnedStreamIsDisposed()
    {
        var stream = new TrackingStream([1, 2, 3]);
        using var httpClient = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stream)
        });
        var sut = new BentoMediaServerService(httpClient);

        var result = await sut.ProxyAsync("https://media.example", "https://video.example/file.mp4");

        Assert.True(result.IsSuccess);
        Assert.False(stream.WasDisposed);
        await result.Value.DisposeAsync();
        Assert.True(stream.WasDisposed);
    }

    [Fact]
    public async Task BentoProxyStream_DisposesResponseContentWhenRequestFails()
    {
        var stream = new TrackingStream([1, 2, 3]);
        using var httpClient = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StreamContent(stream)
        });
        var sut = new BentoMediaServerService(httpClient);

        var result = await sut.ProxyAsync("https://media.example", "https://video.example/file.mp4");

        Assert.True(result.IsFailure);
        Assert.True(stream.WasDisposed);
    }
}
