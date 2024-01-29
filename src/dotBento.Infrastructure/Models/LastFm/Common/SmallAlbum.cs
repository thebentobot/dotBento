using System.Text.Json.Serialization;

namespace dotBento.Infrastructure.Models.LastFm.Common;

public sealed record SmallAlbum(string? Mbid, [property: JsonPropertyName("#text")] string Text);