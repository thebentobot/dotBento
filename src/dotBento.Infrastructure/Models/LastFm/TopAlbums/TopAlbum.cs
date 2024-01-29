using System.Text.Json.Serialization;
using dotBento.Infrastructure.Models.LastFm.Common;
using dotBento.Infrastructure.Models.LastFm.TopTracks;

namespace dotBento.Infrastructure.Models.LastFm.TopAlbums;

public sealed record TopAlbum(
    string? Mbid,
    string Name,
    List<Image> Image,
    SmallArtist Artist,
    Uri Url,
    [property: JsonPropertyName("@attr")] RankAttribute RankAttribute,
    string PlayCount);