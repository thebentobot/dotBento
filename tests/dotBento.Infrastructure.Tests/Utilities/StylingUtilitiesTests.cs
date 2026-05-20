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
    public void MaxLengthStream_Throws_WhenReadBytesExceedLimit()
    {
        using var innerStream = new MemoryStream(new byte[4]);
        using var stream = new StylingUtilities.MaxLengthStream(innerStream, maxBytes: 3);

        var buffer = new byte[4];
        var exception = Assert.Throws<InvalidOperationException>(() => stream.Read(buffer, 0, buffer.Length));

        Assert.Contains("Image is too large", exception.Message);
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
