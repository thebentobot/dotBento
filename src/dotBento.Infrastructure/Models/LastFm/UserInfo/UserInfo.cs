using System.Text.Json.Serialization;
using dotBento.Infrastructure.Models.LastFm.Common;

namespace dotBento.Infrastructure.Models.LastFm.UserInfo;

public sealed record UserInfo(
    string Name,
    string Age,
    string Subscriber,
    string RealName,
    string Bootstrap,
    string PlayCount,
    [property: JsonPropertyName("artist_count")] string ArtistCount,
    string Playlists,
    [property: JsonPropertyName("track_count")] string TrackCount,
    [property: JsonPropertyName("album_count")] string AlbumCount,
    List<Image> Image,
    UserInfoRegistered Registered,
    string Country,
    string Gender,
    Uri Url,
    string Type
);