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

    [Fact]
    public async Task SushiiImageServerStream_DelegatesSyncMembersAndRejectsWrites()
    {
        var contentStream = new CountingStream([1, 2, 3, 4]);
        using var httpClient = CreateHttpClient(contentStream);
        var service = new SushiiImageServerService(httpClient);

        var result = await service.GetSushiiImage("https://image.example/render", "<html></html>", 600, 400);

        Assert.True(result.IsSuccess);
        using var stream = result.Value;
        Assert.True(stream.CanRead);
        Assert.True(stream.CanSeek);
        Assert.False(stream.CanWrite);
        Assert.Equal(4, stream.Length);
        Assert.Equal(0, stream.Position);

        stream.Position = 1;
        Assert.Equal(1, stream.Position);
        Assert.Equal(2, stream.ReadByte());
        Assert.Equal(0, stream.Seek(0, SeekOrigin.Begin));
        stream.Flush();
        await stream.FlushAsync(TestContext.Current.CancellationToken);

        Assert.Throws<NotSupportedException>(() => stream.SetLength(10));
        Assert.Throws<NotSupportedException>(() => stream.Write([1], 0, 1));
        Assert.Throws<NotSupportedException>(() => stream.Write([1]));
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            stream.WriteAsync([1], 0, 1, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await stream.WriteAsync(new ReadOnlyMemory<byte>([1]), TestContext.Current.CancellationToken));
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
