using System.Net;
using dotBento.Infrastructure.Services.Api;
using Moq;
using Moq.Protected;

namespace dotBento.Infrastructure.Tests.Services.Api;

public sealed class BentoMediaServerServiceTests
{
    [Fact]
    public async Task GetHoroscopeAsync_DeserializesContractAndSendsApiKey()
    {
        HttpRequestMessage? request = null;
        using var httpClient = CreateHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("""
            {
              "zodiac": "libra",
              "window": "tomorrow",
              "label": "Aug 9, 2026",
              "reading": "Choose balance.",
              "aspects": [{ "name": "Romance", "score": 4, "detail": "Be open." }]
            }
            """)
        }, message => request = message);

        var result = await new BentoMediaServerService(httpClient).GetHoroscopeAsync(
            "https://media.example/",
            "libra",
            "tomorrow",
            "secret");

        Assert.True(result.IsSuccess);
        Assert.Equal("Choose balance.", result.Value.Reading);
        Assert.Equal(4, Assert.Single(result.Value.Aspects).Score);
        Assert.Equal("secret", request!.Headers.GetValues("X-API-Key").Single());
        Assert.Equal(
            "https://media.example/horoscope/libra?window=tomorrow",
            request.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetHoroscopeAsync_ReturnsFailureForHttpErrorNullPayloadAndException()
    {
        using var errorClient = CreateHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadGateway,
            Content = new StringContent("upstream down")
        });
        using var nullClient = CreateHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("null")
        });
        using var throwingClient = CreateHttpClient(new HttpRequestException("offline"));

        var httpError = await new BentoMediaServerService(errorClient)
            .GetHoroscopeAsync("https://media.example", "aries", "today");
        var nullPayload = await new BentoMediaServerService(nullClient)
            .GetHoroscopeAsync("https://media.example", "aries", "today");
        var exception = await new BentoMediaServerService(throwingClient)
            .GetHoroscopeAsync("https://media.example", "aries", "today");

        Assert.Contains("502", httpError.Error);
        Assert.Equal("Empty response from media server", nullPayload.Error);
        Assert.Contains("offline", exception.Error);
    }

    [Fact]
    public async Task GetHoroscopeAsync_OmitsApiKeyWhenNotConfigured()
    {
        HttpRequestMessage? request = null;
        using var httpClient = CreateHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("""
            { "zodiac": "aries", "window": "weekly", "label": "This week", "reading": "Act.", "aspects": [] }
            """)
        }, message => request = message);

        var result = await new BentoMediaServerService(httpClient)
            .GetHoroscopeAsync("https://media.example", "aries", "weekly");

        Assert.True(result.IsSuccess);
        Assert.False(request!.Headers.Contains("X-API-Key"));
    }

    [Fact]
    public async Task ResolveAsync_ReturnsResponseAndSendsApiKey()
    {
        HttpRequestMessage? request = null;
        using var httpClient = CreateHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("""
            {
              "platform": "youtube",
              "source_url": "https://source.example/watch",
              "posted_at": null,
              "author": {
                "username": "author",
                "display_name": "Author"
              },
              "content": {
                "caption": "caption",
                "attachments": [
                  {
                    "type": "video",
                    "url": "https://cdn.example/video.mp4",
                    "content_type": "video/mp4",
                    "proxy": true
                  }
                ]
              }
            }
            """)
        }, message => request = message);
        var service = new BentoMediaServerService(httpClient);

        var result = await service.ResolveAsync("https://media.example", "https://source.example/watch", "secret");

        Assert.True(result.IsSuccess);
        Assert.Equal("https://cdn.example/video.mp4", Assert.Single(result.Value.Content.Attachments).Url);
        Assert.Equal("secret", request!.Headers.GetValues("X-API-Key").Single());
        Assert.Equal("https://media.example/resolve", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task ResolveAsync_ReturnsFailureForHttpErrorNullPayloadAndException()
    {
        using var errorClient = CreateHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadGateway,
            Content = new StringContent("upstream down")
        });
        using var nullClient = CreateHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("null")
        });
        using var throwingClient = CreateHttpClient(new HttpRequestException("offline"));

        var httpError = await new BentoMediaServerService(errorClient).ResolveAsync("https://media.example", "url");
        var nullPayload = await new BentoMediaServerService(nullClient).ResolveAsync("https://media.example", "url");
        var exception = await new BentoMediaServerService(throwingClient).ResolveAsync("https://media.example", "url");

        Assert.True(httpError.IsFailure);
        Assert.Contains("502", httpError.Error);
        Assert.True(nullPayload.IsFailure);
        Assert.Equal("Empty response from media server", nullPayload.Error);
        Assert.True(exception.IsFailure);
        Assert.Contains("offline", exception.Error);
    }

    [Fact]
    public async Task ProxyAsync_ReturnsFailureForHttpErrorAndException()
    {
        using var errorClient = CreateHttpClient(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });
        using var throwingClient = CreateHttpClient(new HttpRequestException("offline"));

        var httpError = await new BentoMediaServerService(errorClient).ProxyAsync("https://media.example", "url");
        var exception = await new BentoMediaServerService(throwingClient).ProxyAsync("https://media.example", "url");

        Assert.True(httpError.IsFailure);
        Assert.Contains("404", httpError.Error);
        Assert.True(exception.IsFailure);
        Assert.Contains("offline", exception.Error);
    }

    [Fact]
    public async Task ProxyAsync_ReturnsStreamAndSendsApiKey()
    {
        HttpRequestMessage? request = null;
        using var httpClient = CreateHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("proxied media")
        }, message => request = message);
        var service = new BentoMediaServerService(httpClient);

        var result = await service.ProxyAsync(
            "https://media.example",
            "https://source.example/watch?v=1&name=bento",
            "secret");

        Assert.True(result.IsSuccess);
        await using var stream = result.Value;
        using var reader = new StreamReader(stream);
        Assert.Equal("proxied media", await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
        Assert.Equal("secret", request!.Headers.GetValues("X-API-Key").Single());
        Assert.Equal(
            "https://media.example/proxy?url=https%3A%2F%2Fsource.example%2Fwatch%3Fv%3D1%26name%3Dbento",
            request.RequestUri!.ToString());
    }

    [Fact]
    public async Task ProxyAsync_DisposesResponseAndReturnsFailureWhenStreamCreationFails()
    {
        using var httpClient = CreateHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new ThrowingStreamContent()
        });
        var service = new BentoMediaServerService(httpClient);

        var result = await service.ProxyAsync("https://media.example", "url");

        Assert.True(result.IsFailure);
        Assert.Contains("stream failed", result.Error);
    }

    private static HttpClient CreateHttpClient(HttpResponseMessage response, Action<HttpRequestMessage>? captureRequest = null)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                captureRequest?.Invoke(request);
                return response;
            });

        return new HttpClient(mockHandler.Object);
    }

    private static HttpClient CreateHttpClient(Exception exception)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);

        return new HttpClient(mockHandler.Object);
    }

    private sealed class ThrowingStreamContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => Task.CompletedTask;

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return true;
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
            => throw new InvalidOperationException("stream failed");
    }
}
