using System.Text.Json.Serialization;

namespace dotBento.Infrastructure.Models.Weather;

public sealed record OpenWeatherApiMainObject(
    double Temp,
    [property: JsonPropertyName("feels_like")] double FeelsLike,
    [property: JsonPropertyName("temp_min")] double TempMin,
    [property: JsonPropertyName("temp_max")] double TempMax,
    int Pressure,
    int Humidity);