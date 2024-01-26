namespace dotBento.Infrastructure.Models.Weather;

public record OpenWeatherApiWeatherObject
{
    public int Id { get; init; }
    public string Main { get; init; }
    public string Description { get; init; }
    public string Icon { get; init; }
}