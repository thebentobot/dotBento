using System.Net;
using dotBento.Infrastructure.Services.Api;
using Moq;
using Moq.Protected;

namespace dotBento.Infrastructure.Tests.Services.Api;

public sealed class StreamingApiServiceTests
{
    [Fact]
    public async Task BentoMediaProxyAsync_ReturnsStreamWithoutBufferingResponseBody()
    {
        var contentStream = new CountingStream(new byte[1024 * 1024]);
        using var httpClient = CreateHttpClient(contentStream);
        var service = new BentoMediaServerService(httpClient);

        var result = await service.ProxyAsync("https://media.example", "https://cdn.example/video.mp4");

        Assert.True(result.IsSuccess);
        Assert.Equal(0, contentStream.ReadCount);

        var buffer = new byte[1024];
        var read = await result.Value.ReadAsync(buffer.AsMemory(), TestContext.Current.CancellationToken);

        Assert.Equal(buffer.Length, read);
        Assert.True(contentStream.ReadCount > 0);

        await result.Value.DisposeAsync();
        Assert.True(contentStream.IsDisposed);
    }

    [Fact]
    public async Task SushiiImageServer_ReturnsStreamWithoutBufferingResponseBody()
    {
        var contentStream = new CountingStream(new byte[1024 * 1024]);
        using var httpClient = CreateHttpClient(contentStream);
        var service = new SushiiImageServerService(httpClient);

        var result = await service.GetSushiiImage("https://image.example/render", "<html></html>", 600, 400);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, contentStream.ReadCount);

        var buffer = new byte[1024];
        var read = await result.Value.ReadAsync(buffer.AsMemory(), TestContext.Current.CancellationToken);

        Assert.Equal(buffer.Length, read);
        Assert.True(contentStream.ReadCount > 0);

        await result.Value.DisposeAsync();
        Assert.True(contentStream.IsDisposed);
    }

    private static HttpClient CreateHttpClient(Stream contentStream)
    {
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(contentStream)
            });

        return new HttpClient(mockHandler.Object);
    }

    private sealed class CountingStream(byte[] buffer) : MemoryStream(buffer)
    {
        public int ReadCount { get; private set; }
        public bool IsDisposed { get; private set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCount++;
            return base.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            ReadCount++;
            return base.Read(buffer);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ReadCount++;
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return base.ReadAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            IsDisposed = true;
            await base.DisposeAsync();
        }
    }
}
