using System.Text.Json.Serialization;
using dotBento.Infrastructure.Models.LastFm.Common;

namespace dotBento.Infrastructure.Models.LastFm.RecentTracks;

public sealed record RecentTracksWithUserAttributes(
    [property: JsonPropertyName("@attr")] Attributes Attributes,
    IReadOnlyList<RecentTrack> Track);