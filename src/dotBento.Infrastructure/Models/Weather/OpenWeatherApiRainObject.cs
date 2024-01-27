using System.Text.Json.Serialization;

namespace dotBento.Infrastructure.Models.Weather;

public sealed record OpenWeatherApiRainObject(
    [property: JsonPropertyName("1h")] double? OneHour,
    [property: JsonPropertyName("3h")] double? ThreeHours);