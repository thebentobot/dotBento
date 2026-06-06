using System.Reflection;
using System.Net;
using dotBento.Infrastructure.Utilities;
using Moq;
using Moq.Protected;
using SkiaSharp;

namespace dotBento.Infrastructure.Tests.Utilities;

public class StylingUtilitiesTests
{
    [Fact]
    public async Task TryGetDominantColorAsync_ReturnsSuccess_WhenImageIsValid()
    {
        // Arrange
        using var bitmap = new SKBitmap(2, 2);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var imageStream = new MemoryStream();
        data.SaveTo(imageStream);
        imageStream.Position = 0;

        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(imageStream)
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var utilities = new StylingUtilities(httpClient);

        // Act
        var result = await utilities.TryGetDominantColorAsync("http://fake-image-url");

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TryGetDominantColorAsync_ReturnsFailure_WhenStatusCodeFails()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });

        var httpClient = new HttpClient(mockHandler.Object);
        var utilities = new StylingUtilities(httpClient);

        var result = await utilities.TryGetDominantColorAsync("http://fake-image-url");

        Assert.True(result.IsFailure);
        Assert.Contains("Status code: 404", result.Error);
    }

    [Fact]
    public async Task TryGetDominantColorAsync_ReturnsFailure_WhenImageCannotBeDecoded()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent([1, 2, 3])
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var utilities = new StylingUtilities(httpClient);

        var result = await utilities.TryGetDominantColorAsync("http://fake-image-url");

        Assert.True(result.IsFailure);
        Assert.Contains("Could not decode image stream", result.Error);
    }

    [Fact]
    public async Task TryGetDominantColorAsync_ReturnsFailure_WhenHttpThrows()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("something went wrong"));

        var httpClient = new HttpClient(mockHandler.Object);
        var utilities = new StylingUtilities(httpClient);

        // Act
        var result = await utilities.TryGetDominantColorAsync("http://fake-image-url");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("something went wrong", result.Error);
    }

    [Fact]
    public async Task TryGetDominantColorAsync_ReturnsFailure_WhenContentLengthExceedsLimit()
    {
        var mockHandler = new Mock<HttpMessageHandler>();

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new ByteArrayContent([])
        };
        response.Content.Headers.ContentLength = StylingUtilities.MaxImageBytes + 1;

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHandler.Object);
        var utilities = new StylingUtilities(httpClient);

        var result = await utilities.TryGetDominantColorAsync("http://fake-image-url");

        Assert.True(result.IsFailure);
        Assert.Contains("Image is too large", result.Error);
    }

    [Fact]
    public void MaxLengthStream_DelegatesOperationsAndUnsupportedMembersThrow()
    {
        using var innerStream = new MemoryStream([1, 2, 3]);
        using var stream = new StylingUtilities.MaxLengthStream(innerStream, maxBytes: 10);

        Assert.True(stream.CanRead);
        Assert.False(stream.CanSeek);
        Assert.False(stream.CanWrite);
        Assert.Throws<NotSupportedException>(() => stream.Length);
        Assert.Equal(0, stream.Position);
        Assert.Throws<NotSupportedException>(() => stream.Position = 1);
        Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => stream.SetLength(1));
        Assert.Throws<NotSupportedException>(() => stream.Write([1], 0, 1));
        Assert.Throws<NotSupportedException>(() => stream.Write([1]));

        stream.Flush();
        Assert.Equal(1, stream.Read(new byte[1], 0, 1));
        Assert.Equal(1, stream.Position);
        Assert.Equal(1, stream.Read(new Span<byte>(new byte[1])));
        Assert.Equal(2, stream.Position);
    }

    [Fact]
    public void MaxLengthStream_DisposeDisposesInnerStream()
    {
        var innerStream = new TrackingMemoryStream([1, 2, 3]);
        using (new StylingUtilities.MaxLengthStream(innerStream, maxBytes: 10))
        {
        }

        Assert.True(innerStream.IsDisposed);
    }

    [Fact]
    public void MaxLengthStream_Throws_WhenReadBytesExceedLimit()
    {
        using var innerStream = new MemoryStream(new byte[4]);
        using var stream = new StylingUtilities.MaxLengthStream(innerStream, maxBytes: 3);

        var buffer = new byte[4];
        var exception = Assert.Throws<InvalidOperationException>(() => stream.Read(buffer, 0, buffer.Length));

        Assert.Contains("Image is too large", exception.Message);
    }

    [Fact]
    public async Task MaxLengthStream_TracksAsyncReadsAndDisposeAsync()
    {
        var innerStream = new TrackingMemoryStream([1, 2, 3, 4]);
        await using var stream = new StylingUtilities.MaxLengthStream(innerStream, maxBytes: 10);

        var arrayBuffer = new byte[2];
        var arrayRead = await stream.ReadAsync(arrayBuffer, 0, arrayBuffer.Length, TestContext.Current.CancellationToken);
        var memoryRead = await stream.ReadAsync(new Memory<byte>(new byte[2]), TestContext.Current.CancellationToken);

        Assert.Equal(2, arrayRead);
        Assert.Equal(2, memoryRead);
        Assert.Equal(4, stream.Position);

        await stream.DisposeAsync();
        Assert.True(innerStream.IsAsyncDisposed);
    }

    [Fact]
    public void GetSampleInfo_ScalesLargeImagesToMaxDimension()
    {
        var method = typeof(StylingUtilities)
            .GetMethod("GetSampleInfo", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = (SKImageInfo)method.Invoke(null, [new SKImageInfo(512, 256)])!;

        Assert.Equal(128, result.Width);
        Assert.Equal(64, result.Height);
        Assert.Equal(SKColorType.Rgba8888, result.ColorType);
        Assert.Equal(SKAlphaType.Premul, result.AlphaType);
    }

    [Fact]
    public void CalculateDominantColor_ReturnsAverageColor()
    {
        // Arrange
        using var bitmap = new SKBitmap(2, 2);
        bitmap.SetPixel(0, 0, new SKColor(100, 150, 200));
        bitmap.SetPixel(0, 1, new SKColor(100, 150, 200));
        bitmap.SetPixel(1, 0, new SKColor(200, 100, 50));
        bitmap.SetPixel(1, 1, new SKColor(200, 100, 50));

        // Act
        var result = StylingUtilities.CalculateDominantColor(bitmap);

        // Assert
        Assert.Equal(System.Drawing.Color.FromArgb(150, 125, 125), result);
    }

}

internal sealed class TrackingMemoryStream(byte[] buffer) : MemoryStream(buffer)
{
    public bool IsAsyncDisposed { get; private set; }
    public bool IsDisposed { get; private set; }

    public override async ValueTask DisposeAsync()
    {
        IsAsyncDisposed = true;
        await base.DisposeAsync();
    }

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}
