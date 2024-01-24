using System.Text.Json.Serialization;

namespace dotBento.Infrastructure.Models.Weather;

public record OpenWeatherApiRainObject
{
    [property: JsonPropertyName("1h")] public double? OneHour { get; init; }
    [property: JsonPropertyName("3h")] public double? ThreeHours { get; init; }
}