using System.Net;
using dotBento.Infrastructure.Models.LastFm;
using dotBento.Infrastructure.Services.Api;
using Moq;
using Moq.Protected;

namespace dotBento.Infrastructure.Tests.Services.Api;

public sealed class LastFmApiServiceTests
{
    [Fact]
    public async Task LastFmApiService_DeserializesSuccessfulResponses()
    {
        using var httpClient = CreateHttpClient(
            JsonResponse("""{ "recenttracks": { "track": [] } }"""),
            JsonResponse("""{ "toptracks": { "track": [] } }"""),
            JsonResponse("""{ "topalbums": { "album": [] } }"""),
            JsonResponse("""{ "topartists": { "artist": [] } }"""),
            JsonResponse("""{ "user": { "name": "listener" } }"""));
        var service = new LastFmApiService(httpClient);

        var recentTracks = await service.GetRecentTracks("listener", "api-key", 10);
        var topTracks = await service.GetTopTracks("listener", "api-key", "7day", 10);
        var topAlbums = await service.GetTopAlbums("listener", "api-key", "1month", 10);
        var topArtists = await service.GetTopArtists("listener", "api-key", "12month", 10);
        var userInfo = await service.GetUserInfo("listener", "api-key");

        Assert.True(recentTracks.IsSuccess);
        Assert.NotNull(recentTracks.Value.RecentTracks);
        Assert.True(topTracks.IsSuccess);
        Assert.NotNull(topTracks.Value.TopTracks);
        Assert.True(topAlbums.IsSuccess);
        Assert.NotNull(topAlbums.Value.TopAlbums);
        Assert.True(topArtists.IsSuccess);
        Assert.NotNull(topArtists.Value.TopArtists);
        Assert.True(userInfo.IsSuccess);
        Assert.Equal("listener", userInfo.Value.User.Name);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "400 Bad Request")]
    [InlineData(HttpStatusCode.Forbidden, "403 Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "404 Not Found")]
    [InlineData(HttpStatusCode.InternalServerError, "500 Internal Server Error")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "503 Service Unavailable")]
    [InlineData(HttpStatusCode.TooManyRequests, "429 status code")]
    public async Task GetRecentTracks_ReturnsFriendlyFailureForErrorStatus(
        HttpStatusCode statusCode,
        string expectedError)
    {
        using var httpClient = CreateHttpClient(new HttpResponseMessage { StatusCode = statusCode });
        var service = new LastFmApiService(httpClient);

        var result = await service.GetRecentTracks("missing-user", "api-key");

        Assert.True(result.IsFailure);
        Assert.Contains(expectedError, result.Error);
    }

    [Fact]
    public async Task LastFmApiService_ReturnsFailureForNullPayloads()
    {
        using var httpClient = CreateHttpClient(
            JsonResponse("null"),
            JsonResponse("null"),
            JsonResponse("null"),
            JsonResponse("null"),
            JsonResponse("null"));
        var service = new LastFmApiService(httpClient);

        var recentTracks = await service.GetRecentTracks("listener", "api-key");
        var topTracks = await service.GetTopTracks("listener", "api-key", "7day");
        var topAlbums = await service.GetTopAlbums("listener", "api-key", "7day");
        var topArtists = await service.GetTopArtists("listener", "api-key", "7day");
        var userInfo = await service.GetUserInfo("listener", "api-key");

        Assert.True(recentTracks.IsFailure);
        Assert.True(topTracks.IsFailure);
        Assert.True(topAlbums.IsFailure);
        Assert.True(topArtists.IsFailure);
        Assert.True(userInfo.IsFailure);
    }

    [Fact]
    public async Task GetTopTracks_SendsExpectedQueryParameters()
    {
        HttpRequestMessage? request = null;
        using var httpClient = CreateHttpClient(
            JsonResponse("""{ "toptracks": { "track": [] } }"""),
            message => request = message);
        var service = new LastFmApiService(httpClient);

        var result = await service.GetTopTracks("listener name", "api-key", "overall", 25);

        Assert.True(result.IsSuccess);
        Assert.NotNull(request);
        var query = request!.RequestUri!.Query;
        Assert.Contains($"method={Uri.EscapeDataString(ApiMethod.TopTracks)}", query);
        Assert.Contains("user=listener%20name", query);
        Assert.Contains("api_key=api-key", query);
        Assert.Contains("period=overall", query);
        Assert.Contains("limit=25", query);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json)
        };

    private static HttpClient CreateHttpClient(params HttpResponseMessage[] responses) =>
        CreateHttpClient(responses, _ => { });

    private static HttpClient CreateHttpClient(HttpResponseMessage response, Action<HttpRequestMessage> captureRequest) =>
        CreateHttpClient([response], captureRequest);

    private static HttpClient CreateHttpClient(HttpResponseMessage[] responses, Action<HttpRequestMessage> captureRequest)
    {
        var queue = new Queue<HttpResponseMessage>(responses);
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                captureRequest(request);
                return queue.Dequeue();
            });

        return new HttpClient(mockHandler.Object);
    }
}
