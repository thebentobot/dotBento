using System.Text.Json.Serialization;
using dotBento.Infrastructure.Models.LastFm.Common;

namespace dotBento.Infrastructure.Models.LastFm.TopArtists;

public sealed record TopArtistsWithUserAttributes(
    IReadOnlyList<TopArtist> Artist,
    [property: JsonPropertyName("@attr")] Attributes Attributes);