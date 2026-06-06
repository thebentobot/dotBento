using System.Net;
using dotBento.Infrastructure.Services.Api;
using Moq;
using Moq.Protected;

namespace dotBento.Infrastructure.Tests.Services.Api;

public class HttpApiServiceTests
{
    [Fact]
    public async Task UrbanDictionaryService_DeserializesDefinitions()
    {
        using var httpClient = CreateHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("""
            {
              "list": [
                {
                  "definition": "meaning",
                  "permalink": "https://urban.example/word",
                  "thumbs_up": 10,
                  "thumbs_down": 1,
                  "author": "author",
                  "word": "word",
                  "written_on": "2026-01-01T00:00:00Z",
                  "defid": 123,
                  "example": "example"
                }
              ]
            }
            """)
        });
        var service = new UrbanDictionaryService(httpClient);

        var result = await service.GetDefinition("word");

        var definition = Assert.Single(result!.List);
        Assert.Equal("meaning", definition.Definition);
        Assert.Equal(10, definition.ThumbsUp);
    }

    [Fact]
    public async Task WeatherApiService_ReturnsSuccessForValidPayload()
    {
        using var httpClient = CreateHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("""
            {
              "name": "Copenhagen",
              "id": 1,
              "cod": 200,
              "message": null,
              "weather": [{ "id": 800, "main": "Clear", "description": "clear sky", "icon": "01d" }],
              "sys": { "country": "DK", "sunrise": 1, "sunset": 2, "id": 3, "type": 4 },
              "main": { "temp": 20, "feels_like": 19, "temp_min": 18, "temp_max": 21, "pressure": 1000, "humidity": 50 },
              "dt": 1,
              "timezone": 3600,
              "visibility": 10000,
              "base": "stations",
              "clouds": { "all": 0 },
              "wind": { "speed": 1.5, "deg": 180 },
              "coord": { "lon": 12.5, "lat": 55.6 },
              "rain": null,
              "snow": null
            }
            """)
        });
        var service = new WeatherApiService(httpClient);

        var result = await service.GetWeatherForCity("Copenhagen", "key");

        Assert.True(result.IsSuccess);
        Assert.Equal("Copenhagen", result.Value.Name);
        Assert.Equal("DK", result.Value.Sys.Country);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Status Code 400")]
    [InlineData(HttpStatusCode.Unauthorized, "Status Code 401")]
    [InlineData(HttpStatusCode.NotFound, "Could not find the city")]
    [InlineData((HttpStatusCode)429, "too many requests")]
    [InlineData(HttpStatusCode.InternalServerError, "Internal server error")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Service unavailable")]
    [InlineData(HttpStatusCode.Forbidden, "Unknown error")]
    public async Task WeatherApiService_ReturnsFriendlyFailureForErrorStatus(
        HttpStatusCode statusCode,
        string expectedError)
    {
        using var httpClient = CreateHttpClient(new HttpResponseMessage { StatusCode = statusCode });
        var service = new WeatherApiService(httpClient);

        var result = await service.GetWeatherForCity("Atlantis", "key");

        Assert.True(result.IsFailure);
        Assert.Contains(expectedError, result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WeatherApiService_ReturnsFailureForNullPayload()
    {
        using var httpClient = CreateHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("null")
        });
        var service = new WeatherApiService(httpClient);

        var result = await service.GetWeatherForCity("Copenhagen", "key");

        Assert.True(result.IsFailure);
        Assert.Contains("Could not deserialize", result.Error);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, true, null)]
    [InlineData(HttpStatusCode.NotFound, false, null)]
    [InlineData(HttpStatusCode.Forbidden, false, "Discord API returned 403")]
    public async Task DiscordApiService_MapsStatusCodes(
        HttpStatusCode statusCode,
        bool expectedValue,
        string? expectedError)
    {
        using var httpClient = CreateHttpClient(new HttpResponseMessage { StatusCode = statusCode });
        httpClient.BaseAddress = new Uri("https://discord.example/");
        var service = new DiscordApiService(httpClient);

        var result = await service.GetGuildMemberAsync(1, 2);

        if (expectedError is null)
        {
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedValue, result.Value);
        }
        else
        {
            Assert.True(result.IsFailure);
            Assert.Equal(expectedError, result.Error);
        }
    }

    [Fact]
    public async Task DiscordApiService_ReturnsFailureWhenRequestThrows()
    {
        using var httpClient = CreateHttpClient(new HttpRequestException("offline"));
        httpClient.BaseAddress = new Uri("https://discord.example/");
        var service = new DiscordApiService(httpClient);

        var result = await service.GetGuildMemberAsync(1, 2);

        Assert.True(result.IsFailure);
        Assert.Contains("offline", result.Error);
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
}
