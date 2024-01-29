using System.Text.Json.Serialization;

namespace dotBento.Infrastructure.Models.LastFm.RecentTracks;

public sealed record SmallArtistRecentTrack(string? Url, string? Mbid, [property: JsonPropertyName("#text")] string Text);