using CSharpFunctionalExtensions;
using dotBento.Bot.Services;
using Moq;
using SpotifyAPI.Web;

namespace dotBento.Bot.Tests.Services;

public sealed class SpotifyApiServiceTests
{
    [Fact]
    public async Task GetArtist_CachesOnlySelectedResultAndSeparatesArtistAndTrackKeys()
    {
        using var cache = new SpotifyResultCache();
        var search = new Mock<ISearchClient>(MockBehavior.Strict);
        var artist = new FullArtist { Id = "artist-1", Name = "Selected artist" };
        var track = new FullTrack { Id = "track-1", Name = "Selected track" };
        search.Setup(s => s.Item(It.Is<SearchRequest>(r => r.Type == SearchRequest.Types.Artist), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResponse { Artists = new Paging<FullArtist, SearchResponse> { Items = [artist, new FullArtist { Id = "unused" }] } });
        search.Setup(s => s.Item(It.Is<SearchRequest>(r => r.Type == SearchRequest.Types.Track), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResponse { Tracks = new Paging<FullTrack, SearchResponse> { Items = [track] } });
        var spotify = new Mock<ISpotifyClient>();
        spotify.SetupGet(s => s.Search).Returns(search.Object);
        var sut = new SpotifyApiService(spotify.Object, cache);

        Assert.Same(artist, (await sut.GetArtist("song artist:artist")).Value);
        Assert.Same(artist, (await sut.GetArtist("song artist:artist")).Value);
        Assert.Same(track, (await sut.GetTrack("song", "artist")).Value);
        Assert.Equal(2, cache.Count);
        search.Verify(s => s.Item(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetArtist_EmptySearch_IsNotCached()
    {
        using var cache = new SpotifyResultCache();
        var search = new Mock<ISearchClient>();
        search.Setup(s => s.Item(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResponse { Artists = new Paging<FullArtist, SearchResponse> { Items = [] } });
        var spotify = new Mock<ISpotifyClient>();
        spotify.SetupGet(s => s.Search).Returns(search.Object);
        var sut = new SpotifyApiService(spotify.Object, cache);
        Assert.True((await sut.GetArtist("unknown")).IsFailure);
        Assert.True((await sut.GetArtist("unknown")).IsFailure);
        Assert.Equal(0, cache.Count);
        search.Verify(s => s.Item(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Cache_OverCapacity_EvictsOldResultsAndRetainsNewResults()
    {
        using var cache = new SpotifyResultCache();
        for (var i = 0; i < SpotifyResultCache.Capacity + 100; i++)
            await cache.GetOrCreateAsync($"artist:{i}", () => Task.FromResult($"result:{i}".AsMaybe()));

        Assert.InRange(cache.Count, 1, SpotifyResultCache.Capacity);
        var reloaded = false;
        var newest = await cache.GetOrCreateAsync($"artist:{SpotifyResultCache.Capacity + 99}", () =>
        {
            reloaded = true;
            return Task.FromResult("unexpected".AsMaybe());
        });
        Assert.Equal($"result:{SpotifyResultCache.Capacity + 99}", newest.Value);
        Assert.False(reloaded);
        var oldest = await cache.GetOrCreateAsync("artist:0", () => Task.FromResult("reloaded".AsMaybe()));
        Assert.Equal("reloaded", oldest.Value);
    }

    [Fact]
    public async Task Cache_FailedLoad_IsNotRetained()
    {
        using var cache = new SpotifyResultCache();
        await Assert.ThrowsAsync<HttpRequestException>(() => cache.GetOrCreateAsync<string>("artist:error", () => throw new HttpRequestException("offline")));
        Assert.Equal(0, cache.Count);
        Assert.Equal("recovered", (await cache.GetOrCreateAsync("artist:error", () => Task.FromResult("recovered".AsMaybe()))).Value);
    }
}
