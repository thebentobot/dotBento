using System.Text.Json.Serialization;
using dotBento.Infrastructure.Models.LastFm.Common;

namespace dotBento.Infrastructure.Models.LastFm.RecentTracks;

public sealed record RecentTrack(
    [property: JsonPropertyName("@attr")] RecentTrackAttribute? Attributes,
    string? Mbid,
    string? Loved,
    SmallArtistRecentTrack Artist,
    List<Image> Image,
    Date? Date,
    Uri Url,
    string Name,
    SmallAlbum Album);