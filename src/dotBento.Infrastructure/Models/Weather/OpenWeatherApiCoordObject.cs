namespace dotBento.Infrastructure.Models.Weather;

public record OpenWeatherApiCoordObject
{
    public double Lon { get; init; }
    public double Lat { get; init; }
}