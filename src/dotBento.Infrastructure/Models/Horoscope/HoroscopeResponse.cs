using System.Text.Json.Serialization;

namespace dotBento.Infrastructure.Models.Horoscope;

public sealed record HoroscopeResponse(
    [property: JsonPropertyName("zodiac")] string Zodiac,
    [property: JsonPropertyName("window")] string Window,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("reading")] string Reading,
    [property: JsonPropertyName("aspects")] IReadOnlyList<HoroscopeAspect> Aspects);

public sealed record HoroscopeAspect(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("detail")] string Detail);
