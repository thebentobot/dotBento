using System.Net;
using System.Text.Json;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services.Api;
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
        var command = new ImageCommands(new ImageRendererService(httpClient));

        var result = await command.GetColour("https://image.example/render", colour);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedHexInput, result.Value.IsHex);
        await result.Value.Image.DisposeAsync();
    }

    [Theory]
    [InlineData("#ff0000", "<style>* { margin: 0; padding: 0 }</style><div style=\"background-color: rgba(255, 0, 0, 1); width: 200px; height: 200px\"></div>")]
    [InlineData("0, 0, 255", "<style>* { margin: 0; padding: 0 }</style><div style=\"background-color: rgba(0, 0, 255, 1); width: 200px; height: 200px\"></div>")]
    public async Task GetColour_SendsSanitizedColourCardToRenderer(string colour, string expectedHtml)
    {
        string? renderedHtml = null;
        using var httpClient = CreateHttpClient(
            new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream([1])) },
            html => renderedHtml = html);
        var command = new ImageCommands(new ImageRendererService(httpClient));

        var result = await command.GetColour("https://image.example/render", colour);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedHtml, renderedHtml);
        await result.Value.Image.DisposeAsync();
    }

    [Theory]
    [InlineData("not-a-colour", "valid hexcode or RGB colour")]
    [InlineData("999, 0, 0", "valid RGB colour")]
    [InlineData("999,0,0", "valid RGB colour")]
    public async Task GetColour_ReturnsValidationFailureForInvalidColours(string colour, string expectedError)
    {
        using var httpClient = CreateHttpClient(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
        var command = new ImageCommands(new ImageRendererService(httpClient));

        var result = await command.GetColour("https://image.example/render", colour);

        Assert.True(result.IsFailure);
        Assert.Contains(expectedError, result.Error);
    }

    [Fact]
    public async Task GetColour_ReturnsFailureWhenImageServerFails()
    {
        using var httpClient = CreateHttpClient(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });
        var command = new ImageCommands(new ImageRendererService(httpClient));

        var result = await command.GetColour("https://image.example/render", "#ffffff");

        Assert.True(result.IsFailure);
        Assert.Equal("Could not get image from the image renderer", result.Error);
    }

    private static HttpClient CreateHttpClient(HttpResponseMessage response, Action<string>? onRenderedHtml = null)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, _) =>
            {
                if (onRenderedHtml != null && request.Content != null)
                {
                    using var body = JsonDocument.Parse(await request.Content.ReadAsStringAsync());
                    onRenderedHtml(body.RootElement.GetProperty("html").GetString()!);
                }

                return response;
            });

        return new HttpClient(mockHandler.Object);
    }
}
