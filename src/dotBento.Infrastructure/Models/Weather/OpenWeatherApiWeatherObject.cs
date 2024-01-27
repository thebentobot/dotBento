namespace dotBento.Infrastructure.Models.Weather;

public sealed record OpenWeatherApiWeatherObject
(
    int Id,
    string? Main,
    string? Description,
    string? Icon
);