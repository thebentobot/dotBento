using System.Net;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services.Api;
using Ganss.Xss;
using Moq;
using Moq.Protected;

namespace dotBento.Infrastructure.Tests.Commands;

public sealed class ImageCommandsTests
{
    [Theory]
    [InlineData("#ff0000", true)]
    [InlineData("0x00ff00", true)]
    [InlineData("0, 0, 255", false)]
    public async Task GetColour_ReturnsImageForValidColours(string colour, bool expectedHexInput)
    {
        using var httpClient = CreateHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StreamContent(new MemoryStream([1, 2, 3]))
        });
        var command = new ImageCommands(new SushiiImageServerService(httpClient), new HtmlSanitizer());

        var result = await command.GetColour("https://image.example/render", colour);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedHexInput, result.Value.IsHex);
        await result.Value.Image.DisposeAsync();
    }

    [Theory]
    [InlineData("not-a-colour", "valid hexcode or RGB colour")]
    [InlineData("999, 0, 0", "valid hexcode")]
    public async Task GetColour_ReturnsValidationFailureForInvalidColours(string colour, string expectedError)
    {
        using var httpClient = CreateHttpClient(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
        var command = new ImageCommands(new SushiiImageServerService(httpClient), new HtmlSanitizer());

        var result = await command.GetColour("https://image.example/render", colour);

        Assert.True(result.IsFailure);
        Assert.Contains(expectedError, result.Error);
    }

    [Fact]
    public async Task GetColour_ReturnsFailureWhenImageServerFails()
    {
        using var httpClient = CreateHttpClient(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });
        var command = new ImageCommands(new SushiiImageServerService(httpClient), new HtmlSanitizer());

        var result = await command.GetColour("https://image.example/render", "#ffffff");

        Assert.True(result.IsFailure);
        Assert.Equal("Could not get image from Sushii Image Server", result.Error);
    }

    private static HttpClient CreateHttpClient(HttpResponseMessage response)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return new HttpClient(mockHandler.Object);
    }
}
