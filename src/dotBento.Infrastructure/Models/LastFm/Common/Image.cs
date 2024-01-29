using System.Text.Json.Serialization;

namespace dotBento.Infrastructure.Models.LastFm.Common;

public sealed record Image([property: JsonPropertyName("#text")] string Url, string Size);