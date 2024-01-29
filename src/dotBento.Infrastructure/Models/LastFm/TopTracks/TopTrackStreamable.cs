using System.Text.Json.Serialization;

namespace dotBento.Infrastructure.Models.LastFm.TopTracks;

public sealed record TopTrackStreamable([property: JsonPropertyName("#text")] string? Text, string? FullTrack);