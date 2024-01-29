using System.Text.Json.Serialization;
using dotBento.Infrastructure.Models.LastFm.Common;
using dotBento.Infrastructure.Models.LastFm.TopTracks;

namespace dotBento.Infrastructure.Models.LastFm.TopArtists;

public sealed record TopArtist(
    string Streamable,
    string? Mbid,
    string Name,
    List<Image> Image,
    Uri Url,
    [property: JsonPropertyName("@attr")] RankAttribute RankAttribute,
    string PlayCount);