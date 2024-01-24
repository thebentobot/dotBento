namespace dotBento.Infrastructure.Models.Weather;

public record OpenWeatherApiWindObject
{
    public double Speed { get; init; }
    public int Deg { get; init; }
}