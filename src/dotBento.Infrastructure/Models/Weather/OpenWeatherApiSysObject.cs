namespace dotBento.Infrastructure.Models.Weather;

public sealed record OpenWeatherApiSysObject
(
    string Country,
    long Sunrise,
    long Sunset,
    int Id,
    int Type
);