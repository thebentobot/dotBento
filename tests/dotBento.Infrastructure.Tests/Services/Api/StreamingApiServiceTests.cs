using System.Net;
using Discord;
using dotBento.Infrastructure.Services.Api;
using Moq;
using Moq.Protected;

namespace dotBento.Infrastructure.Tests.Services.Api;

public sealed class StreamingApiServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NonSeekableHttpResponse_AllowsInitialDiscordResetWithoutBuffering(bool media)
    {
        byte[] expected = [137, 80, 78, 71, 1, 2, 3, 4];
        var source = new NonSeekableStream(expected);
        using var httpClient = CreateHttpClient(source);
        var result = media
            ? await new BentoMediaServerService(httpClient).ProxyAsync("https://media.example", "https://cdn.example/video.mp4")
            : await new SushiiImageServerService(httpClient).GetSushiiImage("https://image.example/render", "<html></html>", 600, 400);
        Assert.True(result.IsSuccess);
        await using var stream = result.Value;
        Assert.False(stream.CanSeek);
        Assert.Equal(0, source.ReadCount);
        // Discord's FileAttachment constructor attempts this even for network streams.
        stream.Position = 0;
        stream.Position = 0;
        using var attachment = new FileAttachment(stream, media ? "video.mp4" : "profile.png");
        Assert.Equal(0, stream.Position);
        Assert.Equal(0, source.ReadCount);
        using var copy = new MemoryStream();
        await attachment.Stream.CopyToAsync(copy, TestContext.Current.CancellationToken);
        Assert.Equal(expected, copy.ToArray());
        Assert.Equal(expected.Length, stream.Position);
        Assert.Throws<NotSupportedException>(() => stream.Position = 0);
        await stream.DisposeAsync();
        Assert.True(source.IsDisposed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task NonSeekableHttpResponse_TracksPositionAcrossReadOverloads(int overload)
    {
        using var httpClient = CreateHttpClient(new NonSeekableStream([1, 2, 3]));
        var result = await new SushiiImageServerService(httpClient).GetSushiiImage("https://image.example/render", "<html></html>", 600, 400);
        Assert.True(result.IsSuccess);
        await using var stream = result.Value;
        stream.Position = 0;
        var buffer = new byte[2];
        var count = overload switch
        {
            0 => stream.Read(buffer, 0, buffer.Length),
            1 => stream.Read(buffer.AsSpan()),
            2 => await stream.ReadAsync(buffer, 0, buffer.Length, TestContext.Current.CancellationToken),
            _ => await stream.ReadAsync(buffer.AsMemory(), TestContext.Current.CancellationToken)
        };
        Assert.Equal(2, count);
        Assert.Equal(new byte[] { 1, 2 }, buffer);
        Assert.Equal(2, stream.Position);
        Assert.Throws<NotSupportedException>(() => stream.Position = 0);
        Assert.Throws<NotSupportedException>(() => stream.Position = 1);
        Assert.Throws<NotSupportedException>(() => stream.Position = 3);
        Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        Assert.Equal(3, stream.ReadByte());
        Assert.Equal(3, stream.Position);
        Assert.Equal(-1, stream.ReadByte());
        Assert.Equal(3, stream.Position);
    }

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
    public async Task SushiiImageServer_DisposesResponseWhenStreamCreationFails()
    {
        using var httpClient = CreateHttpClient(new ThrowingStreamContent());
        var service = new SushiiImageServerService(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetSushiiImage("https://image.example/render", "<html></html>", 600, 400));
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

    private sealed class NonSeekableStream(byte[] bytes) : MemoryStream(bytes)
    {
        public int ReadCount { get; private set; }
        public bool IsDisposed { get; private set; }
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
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
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return base.ReadAsync(buffer, cancellationToken);
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ReadCount++;
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }
        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
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
