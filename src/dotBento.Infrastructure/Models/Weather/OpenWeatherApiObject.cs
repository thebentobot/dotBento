namespace dotBento.Infrastructure.Models.Weather;

// https://openweathermap.org/current
public sealed record OpenWeatherApiObject(
    string? Name,
    int Id,
    int Cod,
    string? Message,
    List<OpenWeatherApiWeatherObject> Weather,
    OpenWeatherApiSysObject Sys,
    OpenWeatherApiMainObject Main,
    long Dt,
    int Timezone,
    int Visibility,
    string Base,
    OpenWeatherApiCloudsObject Clouds,
    OpenWeatherApiWindObject Wind,
    OpenWeatherApiCoordObject Coord,
    OpenWeatherApiRainObject? Rain,
    OpenWeatherApiSnowObject? Snow);