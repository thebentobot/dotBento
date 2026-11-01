using System.Net;
using dotBento.Infrastructure.Services.Api;
using dotBento.Infrastructure.Services.Assets;
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
    public async Task ImageRenderer_ReturnsStreamWithoutBufferingResponseBody()
    {
        var contentStream = new CountingStream(new byte[1024 * 1024]);
        using var httpClient = CreateHttpClient(contentStream);
        var service = new ImageRendererService(httpClient);

        var result = await service.GetImage("https://image.example/render", "<html></html>", 600, 400);

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
    public async Task ImageRendererStream_DelegatesSyncMembersAndRejectsWrites()
    {
        var contentStream = new CountingStream([1, 2, 3, 4]);
        using var httpClient = CreateHttpClient(contentStream);
        var service = new ImageRendererService(httpClient);

        var result = await service.GetImage("https://image.example/render", "<html></html>", 600, 400);

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
        Assert.Equal(1, stream.Read(new Span<byte>(new byte[1])));
        Assert.Equal(1, await stream.ReadAsync(new byte[1], 0, 1, TestContext.Current.CancellationToken));
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

    [Fact]
    public async Task ImageRenderer_DisposesResponseWhenStreamCreationFails()
    {
        using var httpClient = CreateHttpClient(new ThrowingStreamContent());
        var service = new ImageRendererService(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetImage("https://image.example/render", "<html></html>", 600, 400));
    }

    [Fact]
    public async Task ImageRenderer_InlinesHtmlBeforeSending()
    {
        string? posted = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken token) =>
            {
                posted = await request.Content!.ReadAsStringAsync(token);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent([1, 2, 3])
                };
            });
        using var http = new HttpClient(handler.Object);
        var service = new ImageRendererService(http, new FakeInliner("<html>inlined</html>"));

        var result = await service.GetImage(
            "https://image.example/render",
            "<html>raw</html>",
            100,
            100,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains("inlined", posted);
        Assert.DoesNotContain("raw", posted);
        await result.Value.DisposeAsync();
    }

    private sealed class FakeInliner(string output) : IHtmlAssetInliner
    {
        public Task<string> InlineAsync(string html, CancellationToken cancellationToken = default) =>
            Task.FromResult(output);
    }

    private static HttpClient CreateHttpClient(Stream contentStream)
    {
        return CreateHttpClient(new StreamContent(contentStream));
    }

    private static HttpClient CreateHttpClient(HttpContent content)
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
                Content = content
            });

        return new HttpClient(mockHandler.Object);
    }

    private sealed class ThrowingStreamContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            Task.CompletedTask;

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return true;
        }

        protected override Task<Stream> CreateContentReadStreamAsync() =>
            throw new InvalidOperationException("stream failed");
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
