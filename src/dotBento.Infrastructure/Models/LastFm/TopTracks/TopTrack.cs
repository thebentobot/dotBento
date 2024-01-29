using System.Text.Json.Serialization;
using dotBento.Infrastructure.Models.LastFm.Common;

namespace dotBento.Infrastructure.Models.LastFm.TopTracks;

public sealed record TopTrack(
    TopTrackStreamable Streamable,
    string? Mbid,
    string Name,
    List<Image> Image,
    SmallArtist Artist,
    Uri Url,
    string Duration,
    [property: JsonPropertyName("@attr")] RankAttribute RankAttribute,
    string PlayCount);