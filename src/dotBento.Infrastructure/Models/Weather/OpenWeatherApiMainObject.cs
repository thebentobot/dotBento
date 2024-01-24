using System.Text.Json.Serialization;

namespace dotBento.Infrastructure.Models.Weather;

public record OpenWeatherApiMainObject
{
    public double Temp { get; init; }
    [property: JsonPropertyName("feels_like")] public double FeelsLike { get; init; }
    [property: JsonPropertyName("temp_min")] public double TempMin { get; init; }
    [property: JsonPropertyName("temp_max")] public double TempMax { get; init; }
    public int Pressure { get; init; }
    public int Humidity { get; init; }
}