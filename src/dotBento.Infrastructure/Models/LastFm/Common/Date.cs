using System.Text.Json.Serialization;

namespace dotBento.Infrastructure.Models.LastFm.Common;

public sealed record Date(string Uts, [property: JsonPropertyName("#text")] string JsDate);