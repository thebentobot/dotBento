namespace dotBento.Infrastructure.Models.Weather;

public record OpenWeatherApiSysObject
{
    public string Country { get; init; }
    public long Sunrise { get; init; }
    public long Sunset { get; init; }
    public int Id { get; init; }
    public int Type { get; init; }
}