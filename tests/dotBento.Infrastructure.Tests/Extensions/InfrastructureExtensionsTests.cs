using dotBento.Infrastructure.Extensions;
using dotBento.Infrastructure.Models.LastFm.Common;
using dotBento.Infrastructure.Models.LastFm.RecentTracks;
using dotBento.Infrastructure.Models.LastFm.TopAlbums;
using dotBento.Infrastructure.Models.LastFm.TopArtists;
using dotBento.Infrastructure.Models.LastFm.TopTracks;
using dotBento.Infrastructure.Models.LastFm.UserInfo;
using EfProfile = dotBento.EntityFramework.Entities.Profile;
using EfReminder = dotBento.EntityFramework.Entities.Reminder;
using EfTag = dotBento.EntityFramework.Entities.Tag;

namespace dotBento.Infrastructure.Tests.Extensions;

public class InfrastructureExtensionsTests
{
    private static List<Image> Images() =>
    [
        new Image("small.png", "small"),
        new Image("large.png", "large")
    ];

    [Fact]
    public void ToBentoLastFmRecentTrack_MapsNowPlayingTrack()
    {
        var track = new RecentTrack(
            new RecentTrackAttribute("true"),
            null,
            null,
            new SmallArtistRecentTrack(null, null, "Artist"),
            Images(),
            null,
            new Uri("https://last.fm/track"),
            "Track",
            new SmallAlbum(null, "Album"));

        var result = track.ToBentoLastFmRecentTrack();

        Assert.True(result.NowPlaying);
        Assert.Equal("Artist", result.Artist);
        Assert.Equal("Track", result.Track);
        Assert.Equal("Album", result.Album);
        Assert.Equal("large.png", result.Image);
        Assert.Equal("https://last.fm/track", result.Url);
        Assert.Null(result.Date);
    }

    [Fact]
    public void ToBentoLastFmRecentTrack_MapsHistoricalTrackDate()
    {
        var track = new RecentTrack(
            null,
            null,
            null,
            new SmallArtistRecentTrack(null, null, "Artist"),
            Images(),
            new Date("60", "date"),
            new Uri("https://last.fm/track"),
            "Track",
            new SmallAlbum(null, "Album"));

        var result = track.ToBentoLastFmRecentTrack();

        Assert.False(result.NowPlaying);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(60), result.Date);
    }

    [Fact]
    public void ToBentoLastFmRecentTracksWithTotalTracks_MapsOnlyFirstTwoTracksAndTotal()
    {
        RecentTrack Track(string name) => new(
            null,
            null,
            null,
            new SmallArtistRecentTrack(null, null, "Artist"),
            Images(),
            new Date("60", "date"),
            new Uri($"https://last.fm/{name}"),
            name,
            new SmallAlbum(null, "Album"));
        var tracks = new RecentTracksWithUserAttributes(
            new Attributes("1", "3", "user", "10", "1"),
            [Track("one"), Track("two"), Track("three")]);

        var result = tracks.ToBentoLastFmRecentTracksWithTotalTracks();

        Assert.Equal(3, result.TotalTracks);
        Assert.Equal(["one", "two"], result.RecentTracks.Select(t => t.Track));
    }

    [Fact]
    public void TopLastFmMappers_MapSharedFields()
    {
        var album = new TopAlbum(
            null,
            "Album",
            Images(),
            new SmallArtist(null, null, "Artist"),
            new Uri("https://last.fm/album"),
            new RankAttribute("2"),
            "42");
        var track = new TopTrack(
            new TopTrackStreamable(null, null),
            null,
            "Track",
            Images(),
            new SmallArtist(null, null, "Artist"),
            new Uri("https://last.fm/track"),
            "123",
            new RankAttribute("3"),
            "43");
        var artist = new TopArtist(
            "0",
            null,
            "Artist",
            Images(),
            new Uri("https://last.fm/artist"),
            new RankAttribute("4"),
            "44");

        var mappedAlbum = album.ToBentoLastFmTopAlbum();
        var mappedTrack = track.ToBentoLastFmTopTrack();
        var mappedArtist = artist.ToBentoLastFmTopArtist();

        Assert.Equal(("Album", "Artist", "large.png", "https://last.fm/album", 42, 2),
            (mappedAlbum.Name, mappedAlbum.Artist, mappedAlbum.ImageUrl, mappedAlbum.Url, mappedAlbum.PlayCount, mappedAlbum.Rank));
        Assert.Equal(("Track", "Artist", "large.png", "https://last.fm/track", 43, 3),
            (mappedTrack.Name, mappedTrack.Artist, mappedTrack.ImageUrl, mappedTrack.Url, mappedTrack.PlayCount, mappedTrack.Rank));
        Assert.Equal(("Artist", "large.png", "https://last.fm/artist", 44, 4),
            (mappedArtist.Name, mappedArtist.ImageUrl, mappedArtist.Url, mappedArtist.PlayCount, mappedArtist.Rank));
    }

    [Fact]
    public void ToBentoLastFmUserInfo_MapsAllFields()
    {
        var userInfo = new UserInfo(
            "Name",
            "0",
            "0",
            "Real",
            "0",
            "100",
            "20",
            "0",
            "30",
            "10",
            Images(),
            new UserInfoRegistered("60", 60),
            "DK",
            "n",
            new Uri("https://last.fm/user"),
            "user");

        var result = userInfo.ToBentoLastFmUserInfo();

        Assert.Equal("Name", result.Name);
        Assert.Equal("large.png", result.ImageUrl);
        Assert.Equal("https://last.fm/user", result.Url);
        Assert.Equal("DK", result.Country);
        Assert.Equal(100, result.PlayCount);
        Assert.Equal(20, result.ArtistCount);
        Assert.Equal(10, result.AlbumCount);
        Assert.Equal(30, result.TrackCount);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(60), result.RegisteredAt);
    }

    [Fact]
    public void EntityMappers_MapProfileReminderAndTag()
    {
        var profile = new EfProfile
        {
            UserId = 1,
            LastfmBoard = true,
            XpBoard = false,
            BackgroundUrl = "https://image",
            BackgroundColourOpacity = 80,
            BackgroundColour = "#112233",
            Description = "hello",
            Timezone = "Europe/Copenhagen",
            Birthday = "2000-01-01",
            XpBar2Colour = "#445566"
        };
        var reminderDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var reminder = new EfReminder
        {
            Id = 5,
            UserId = 1,
            Reminder1 = "remember",
            Date = reminderDate
        };
        var tagDate = DateTime.UtcNow;
        var tag = new EfTag
        {
            TagId = 7,
            UserId = 1,
            GuildId = 2,
            Date = tagDate,
            Command = "hello",
            Content = "world",
            Count = 3
        };

        var mappedProfile = profile.Map();
        var mappedReminder = reminder.Map();
        var mappedTag = tag.ToBentoTag();

        Assert.Equal(1, mappedProfile.UserId);
        Assert.True(mappedProfile.LastfmBoard);
        Assert.False(mappedProfile.XpBoard);
        Assert.Equal("https://image", mappedProfile.BackgroundUrl);
        Assert.Equal("#445566", mappedProfile.XpBar2Colour);
        Assert.Equal((5, 1L, "remember", reminderDate),
            (mappedReminder.Id, mappedReminder.UserId, mappedReminder.Content, mappedReminder.Date));
        Assert.Equal((7L, 1L, 2L, tagDate, "hello", "world", 3),
            (mappedTag.TagId, mappedTag.UserId, mappedTag.GuildId, mappedTag.Date, mappedTag.Command, mappedTag.Content, mappedTag.Count));
    }
}
