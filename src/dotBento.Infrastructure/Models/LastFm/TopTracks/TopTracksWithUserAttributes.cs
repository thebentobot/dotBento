using System.Text.Json.Serialization;
using dotBento.Infrastructure.Models.LastFm.Common;

namespace dotBento.Infrastructure.Models.LastFm.TopTracks;

public sealed record TopTracksWithUserAttributes(
    IReadOnlyList<TopTrack> Track,
    [property: JsonPropertyName("@attr")] Attributes Attributes);