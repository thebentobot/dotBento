using System.Net;
using dotBento.Infrastructure.Utilities;
using Moq;
using Moq.Protected;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace dotBento.Infrastructure.Tests.Utilities;

public class StylingUtilitiesTests
{
    [Fact]
    public async Task TryGetDominantColorAsync_ReturnsSuccess_WhenImageIsValid()
    {
        // Arrange
        var image = new Image<Rgba32>(2, 2); // a small blank image
        var imageStream = new MemoryStream();
        await image.SaveAsPngAsync(imageStream);
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
    public void CalculateDominantColor_ReturnsAverageColor()
    {
        // Arrange
        var image = new Image<Rgba32>(2, 2);
        image[0, 0] = new Rgba32(100, 150, 200);
        image[0, 1] = new Rgba32(100, 150, 200);
        image[1, 0] = new Rgba32(200, 100, 50);
        image[1, 1] = new Rgba32(200, 100, 50);

        // Act
        var result = StylingUtilities.CalculateDominantColor(image);

        // Assert
        Assert.Equal(System.Drawing.Color.FromArgb(150, 125, 125), result);
    }
}
