using dotBento.Domain.Entities.LastFm;
using dotBento.Infrastructure.Models.LastFm.RecentTracks;
using dotBento.Infrastructure.Models.LastFm.TopAlbums;
using dotBento.Infrastructure.Models.LastFm.TopArtists;
using dotBento.Infrastructure.Models.LastFm.TopTracks;
using dotBento.Infrastructure.Models.LastFm.UserInfo;

namespace dotBento.Infrastructure.Extensions;

public static class LastFmExtensions
{
    public static BentoLastFmRecentTrack ToBentoLastFmRecentTrack(this RecentTrack recentTrack)
    {
        var nowPlaying = recentTrack.Attributes is { NowPlaying: "true" };
        
        return new BentoLastFmRecentTrack(
            nowPlaying,
            recentTrack.Artist.Text,
            recentTrack.Name,
            recentTrack.Album.Text,
            recentTrack.Image.Last().Url,
            recentTrack.Url.ToString(),
            nowPlaying ? null : DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(recentTrack.Date?.Uts)));
    }

    public static BentoLastFmRecentTracksWithTotalTracks ToBentoLastFmRecentTracksWithTotalTracks(
        this RecentTracksWithUserAttributes recentTracksWithUserAttributes) =>
        new(
            recentTracksWithUserAttributes.Track.Select(x => x.ToBentoLastFmRecentTrack()).Take(2).ToList(),
            Convert.ToInt32(recentTracksWithUserAttributes.Attributes.Total));

    public static BentoLastFmTopAlbum ToBentoLastFmTopAlbum(this TopAlbum topAlbum) =>
        new(
            topAlbum.Name,
            topAlbum.Artist.Name,
            topAlbum.Image.Last().Url,
            topAlbum.Url.ToString(),
            Convert.ToInt32(topAlbum.PlayCount),
            Convert.ToInt32(topAlbum.RankAttribute.Rank));

    public static BentoLastFmTopTrack ToBentoLastFmTopTrack(this TopTrack topTrack) =>
        new(
            topTrack.Name,
            topTrack.Artist.Name,
            topTrack.Image.Last().Url,
            topTrack.Url.ToString(),
            Convert.ToInt32(topTrack.PlayCount),
            Convert.ToInt32(topTrack.RankAttribute.Rank));

    public static BentoLastFmTopArtist ToBentoLastFmTopArtist(this TopArtist topArtist) =>
        new(
            topArtist.Name,
            topArtist.Image.Last().Url,
            topArtist.Url.ToString(),
            Convert.ToInt32(topArtist.PlayCount),
            Convert.ToInt32(topArtist.RankAttribute.Rank));

    public static BentoLastFmUserInfo ToBentoLastFmUserInfo(this UserInfo userInfo) =>
        new(
            userInfo.Name,
            userInfo.Image.Last().Url,
            userInfo.Url.ToString(),
            userInfo.Country,
            Convert.ToInt32(userInfo.PlayCount),
            Convert.ToInt32(userInfo.ArtistCount),
            Convert.ToInt32(userInfo.AlbumCount),
            Convert.ToInt32(userInfo.TrackCount),
            DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(userInfo.Registered.Text)));
}