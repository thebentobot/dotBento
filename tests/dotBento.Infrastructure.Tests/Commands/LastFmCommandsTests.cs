using System.Net;
using dotBento.Domain.Entities.LastFm;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services.Api;
using Moq;
using Moq.Protected;

namespace dotBento.Infrastructure.Tests.Commands;

public sealed class LastFmCommandsTests
{
    [Fact]
    public async Task LastFmCommands_MapSuccessfulApiResponses()
    {
        using var lastFmClient = CreateHttpClient(
            JsonResponse("""
            {
              "topartists": {
                "artist": [
                  {
                    "streamable": "0",
                    "mbid": null,
                    "name": "Artist",
                    "image": [{ "#text": "small.png", "size": "small" }, { "#text": "large.png", "size": "large" }],
                    "url": "https://last.fm/artist",
                    "@attr": { "rank": "1" },
                    "playcount": "12"
                  }
                ],
                "@attr": { "page": "1", "totalPages": "1", "user": "listener", "total": "1", "perPage": "1" }
              }
            }
            """),
            JsonResponse("""
            {
              "topalbums": {
                "album": [
                  {
                    "mbid": null,
                    "name": "Album",
                    "image": [{ "#text": "small.png", "size": "small" }, { "#text": "large.png", "size": "large" }],
                    "artist": { "url": null, "mbid": null, "name": "Artist" },
                    "url": "https://last.fm/album",
                    "@attr": { "rank": "2" },
                    "playcount": "34"
                  }
                ]
              }
            }
            """),
            JsonResponse("""
            {
              "toptracks": {
                "track": [
                  {
                    "streamable": { "#text": "0", "fulltrack": "0" },
                    "mbid": null,
                    "name": "Track",
                    "image": [{ "#text": "small.png", "size": "small" }, { "#text": "large.png", "size": "large" }],
                    "artist": { "url": null, "mbid": null, "name": "Artist" },
                    "url": "https://last.fm/track",
                    "duration": "123",
                    "@attr": { "rank": "3" },
                    "playcount": "56"
                  }
                ],
                "@attr": { "page": "1", "totalPages": "1", "user": "listener", "total": "1", "perPage": "1" }
              }
            }
            """),
            JsonResponse("""
            {
              "recenttracks": {
                "@attr": { "page": "1", "totalPages": "1", "user": "listener", "total": "9", "perPage": "2" },
                "track": [
                  {
                    "@attr": { "nowplaying": "true" },
                    "mbid": null,
                    "loved": null,
                    "artist": { "url": null, "mbid": null, "#text": "Artist" },
                    "image": [{ "#text": "small.png", "size": "small" }, { "#text": "large.png", "size": "large" }],
                    "date": null,
                    "url": "https://last.fm/now",
                    "name": "Now",
                    "album": { "mbid": null, "#text": "Album" }
                  },
                  {
                    "mbid": null,
                    "loved": null,
                    "artist": { "url": null, "mbid": null, "#text": "Artist" },
                    "image": [{ "#text": "small.png", "size": "small" }, { "#text": "large.png", "size": "large" }],
                    "date": { "uts": "60", "#text": "date" },
                    "url": "https://last.fm/old",
                    "name": "Old",
                    "album": { "mbid": null, "#text": "Album" }
                  }
                ]
              }
            }
            """),
            JsonResponse("""
            {
              "recenttracks": {
                "@attr": { "page": "1", "totalPages": "1", "user": "listener", "total": "2", "perPage": "50" },
                "track": [
                  {
                    "mbid": null,
                    "loved": null,
                    "artist": { "url": null, "mbid": null, "#text": "Artist" },
                    "image": [{ "#text": "small.png", "size": "small" }, { "#text": "large.png", "size": "large" }],
                    "date": { "uts": "60", "#text": "date" },
                    "url": "https://last.fm/old",
                    "name": "Old",
                    "album": { "mbid": null, "#text": "Album" }
                  }
                ]
              }
            }
            """),
            JsonResponse("""
            {
              "user": {
                "name": "listener",
                "age": "0",
                "subscriber": "0",
                "realname": "Listener",
                "bootstrap": "0",
                "playcount": "100",
                "artist_count": "20",
                "playlists": "0",
                "track_count": "30",
                "album_count": "10",
                "image": [{ "#text": "small.png", "size": "small" }, { "#text": "large.png", "size": "large" }],
                "registered": { "unixtime": "60", "#text": 60 },
                "country": "DK",
                "gender": "n",
                "url": "https://last.fm/user/listener",
                "type": "user"
              }
            }
            """));
        var command = CreateCommand(lastFmClient);

        var artists = await command.GetTopArtists("listener", "api-key", "7day");
        var albums = await command.GetTopAlbums("listener", "api-key", "7day");
        var tracks = await command.GetTopTracks("listener", "api-key", "7day");
        var nowPlaying = await command.NowPlaying("listener", "api-key");
        var recentTracks = await command.GetRecentTracks("listener", "api-key");
        var userInfo = await command.GetUserInfo("listener", "api-key");

        Assert.True(artists.IsSuccess);
        Assert.Equal(("Artist", "large.png", 12, 1),
            (artists.Value.Single().Name, artists.Value.Single().ImageUrl, artists.Value.Single().PlayCount, artists.Value.Single().Rank));
        Assert.True(albums.IsSuccess);
        Assert.Equal(("Album", "Artist", 34, 2),
            (albums.Value.Single().Name, albums.Value.Single().Artist, albums.Value.Single().PlayCount, albums.Value.Single().Rank));
        Assert.True(tracks.IsSuccess);
        Assert.Equal(("Track", "Artist", 56, 3),
            (tracks.Value.Single().Name, tracks.Value.Single().Artist, tracks.Value.Single().PlayCount, tracks.Value.Single().Rank));
        Assert.True(nowPlaying.IsSuccess);
        Assert.Equal(9, nowPlaying.Value.TotalTracks);
        Assert.Equal(["Now", "Old"], nowPlaying.Value.RecentTracks.Select(track => track.Track));
        Assert.True(recentTracks.IsSuccess);
        Assert.Equal("Old", recentTracks.Value.Single().Track);
        Assert.True(userInfo.IsSuccess);
        Assert.Equal(("listener", "large.png", 100, 20, 10, 30),
            (userInfo.Value.Name, userInfo.Value.ImageUrl, userInfo.Value.PlayCount, userInfo.Value.ArtistCount,
                userInfo.Value.AlbumCount, userInfo.Value.TrackCount));
    }

    [Theory]
    [InlineData(nameof(GetTopArtistsFailure))]
    [InlineData(nameof(GetTopAlbumsFailure))]
    [InlineData(nameof(GetTopTracksFailure))]
    [InlineData(nameof(NowPlayingFailure))]
    [InlineData(nameof(GetRecentTracksFailure))]
    [InlineData(nameof(GetUserInfoFailure))]
    public async Task LastFmCommands_PropagateApiFailures(string method)
    {
        using var lastFmClient = CreateHttpClient(new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden });
        var command = CreateCommand(lastFmClient);

        var result = method switch
        {
            nameof(GetTopArtistsFailure) => (await command.GetTopArtists("listener", "bad-key", "7day")).Error,
            nameof(GetTopAlbumsFailure) => (await command.GetTopAlbums("listener", "bad-key", "7day")).Error,
            nameof(GetTopTracksFailure) => (await command.GetTopTracks("listener", "bad-key", "7day")).Error,
            nameof(NowPlayingFailure) => (await command.NowPlaying("listener", "bad-key")).Error,
            nameof(GetRecentTracksFailure) => (await command.GetRecentTracks("listener", "bad-key")).Error,
            nameof(GetUserInfoFailure) => (await command.GetUserInfo("listener", "bad-key")).Error,
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        Assert.Contains("403 Forbidden", result);
    }

    [Fact]
    public async Task LastFmCommands_ReturnFailuresForMissingPayloadSections()
    {
        using var lastFmClient = CreateHttpClient(
            JsonResponse("""{ "topartists": null }"""),
            JsonResponse("""{ "topalbums": null }"""),
            JsonResponse("""{ "toptracks": null }"""),
            JsonResponse("""{ "recenttracks": null }"""),
            JsonResponse("""{ "recenttracks": null }"""),
            JsonResponse("""{ "user": null }"""));
        var command = CreateCommand(lastFmClient);

        var artists = await command.GetTopArtists("listener", "api-key", "7day");
        var albums = await command.GetTopAlbums("listener", "api-key", "7day");
        var tracks = await command.GetTopTracks("listener", "api-key", "7day");
        var nowPlaying = await command.NowPlaying("listener", "api-key");
        var recentTracks = await command.GetRecentTracks("listener", "api-key");
        var userInfo = await command.GetUserInfo("listener", "api-key");

        Assert.Equal("No top artists found", artists.Error);
        Assert.Equal("No top albums found", albums.Error);
        Assert.Equal("No top tracks found", tracks.Error);
        Assert.Equal("No recent tracks found", nowPlaying.Error);
        Assert.Equal("No recent tracks found", recentTracks.Error);
        Assert.Equal("No user info found", userInfo.Error);
    }

    [Fact]
    public async Task GetLastFmCollageImage_RendersExpectedGridDimensions()
    {
        HttpRequestMessage? request = null;
        string? requestBody = null;
        using var sushiiClient = CreateHttpClient(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(new MemoryStream([1, 2, 3]))
            },
            message =>
            {
                request = message;
                requestBody = message.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken)
                    .GetAwaiter()
                    .GetResult();
            });
        var command = CreateCommand(CreateHttpClient(), sushiiClient);
        var collage = Enumerable.Range(1, 12)
            .Select(i => new BentoLastFmCollage($"https://img.example/{i}.png", $"Artist {i}", i, $"Name {i}"))
            .ToList();

        var result = await command.GetLastFmCollageImage("3x3", collage, "https://image.example/render");

        Assert.True(result.IsSuccess);
        await result.Value.DisposeAsync();
        Assert.NotNull(request);
        Assert.Contains(@"""width"":""900""", requestBody);
        Assert.Contains(@"""height"":""900""", requestBody);
        Assert.Contains("Artist 9", requestBody);
        Assert.DoesNotContain("Artist 10", requestBody);
    }

    [Fact]
    public async Task GetLastFmCollageImage_ReturnsFailureWhenImageServerFails()
    {
        using var sushiiClient = CreateHttpClient(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });
        var command = CreateCommand(CreateHttpClient(), sushiiClient);

        var result = await command.GetLastFmCollageImage(
            "2x2",
            [new BentoLastFmCollage("https://img.example/1.png", "Artist", 1, null)],
            "https://image.example/render");

        Assert.True(result.IsFailure);
        Assert.Equal("Could not get image from Sushii Image Server", result.Error);
    }

    private const string GetTopArtistsFailure = nameof(GetTopArtistsFailure);
    private const string GetTopAlbumsFailure = nameof(GetTopAlbumsFailure);
    private const string GetTopTracksFailure = nameof(GetTopTracksFailure);
    private const string NowPlayingFailure = nameof(NowPlayingFailure);
    private const string GetRecentTracksFailure = nameof(GetRecentTracksFailure);
    private const string GetUserInfoFailure = nameof(GetUserInfoFailure);

    private static LastFmCommands CreateCommand(HttpClient lastFmClient, HttpClient? sushiiClient = null) =>
        new(new LastFmApiService(lastFmClient),
            new SushiiImageServerService(sushiiClient ?? CreateHttpClient()));

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
                return queue.Count > 0
                    ? queue.Dequeue()
                    : new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("{}") };
            });

        return new HttpClient(mockHandler.Object);
    }
}
