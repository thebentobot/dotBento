namespace dotBento.Infrastructure.Models.Weather;

// https://openweathermap.org/current
public record OpenWeatherApiObject
{
    public string Name { get; init; }
    public int Id { get; init; }
    public int Cod { get; init; }
    public List<OpenWeatherApiWeatherObject> Weather { get; init; }
    public OpenWeatherApiSysObject Sys { get; init; }
    public OpenWeatherApiMainObject Main { get; init; }
    public long Dt { get; init; }
    public int Timezone { get; init; }
    public int Visibility { get; init; }
    public string Base { get; init; }
    public OpenWeatherApiCloudsObject Clouds { get; init; }
    public OpenWeatherApiWindObject Wind { get; init; }
    public OpenWeatherApiCoordObject Coord { get; init; }
    public OpenWeatherApiRainObject? Rain { get; init; }
    public OpenWeatherApiSnowObject? Snow { get; init; }
}