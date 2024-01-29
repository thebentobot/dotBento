using System.Text.Json.Serialization;

namespace dotBento.Infrastructure.Models.LastFm.Common;

public sealed record SmallArtist(string? Url, string? Mbid, [property: JsonPropertyName("name")] string Name);