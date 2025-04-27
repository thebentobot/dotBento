using System.Text.Json;
using CSharpFunctionalExtensions;
using dotBento.Domain;
using dotBento.Infrastructure.Models.Weather;
using Microsoft.AspNetCore.WebUtilities;

namespace dotBento.Infrastructure.Services.Api;

public sealed class WeatherApiService(HttpClient httpClient)
{
    public async Task<Result<OpenWeatherApiObject>> GetWeatherForCity(string city, string weatherKey)
    {
        var parameters = new Dictionary<string, string?>
        {
            { "q", city },
            { "units", "metric" },
            { "appid", weatherKey },
            { "lang", "en" }
        };

        var response = await httpClient.GetAsync(
            QueryHelpers.AddQueryString($"https://api.openweathermap.org/data/2.5/weather", parameters));

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<OpenWeatherApiObject>(WeatherApiError((int)response.StatusCode, city));
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };
        var responseModel = JsonSerializer.Deserialize<OpenWeatherApiObject>(responseContent, options);

        if (responseModel == null)
        {
            Statistics.WeatherApiErrors.WithLabels("GetWeatherForCity").Inc();
            return Result.Failure<OpenWeatherApiObject>("Could not deserialize the response from OpenWeather. It might be down.");
        }
        
        Statistics.WeatherApiCalls.WithLabels("GetWeatherForCity").Inc();
        return Result.Success(responseModel);
    }
    
    private static string WeatherApiError(int statusCode, string city) =>
        statusCode switch
        {
            400 => "OpenWeather returned Status Code 400: Bad request.\nThe developers of this bot have been notified of this error and are looking into it.",
            401 => "OpenWeather returned Status Code 401: Unauthorized.\nThe developers of this bot have been notified of this error and are looking into it.",
            404 => $"Could not find the city `{city}` which you were looking for. Perhaps try to add country and/or state e.g. \"Copenhagen, Denmark\".",
            429 => "OpenWeather returned Status Code 429: Too many requests. Please try again later.",
            500 => "OpenWeather returned Status Code 500: Internal server error. Please try again later, hopefully the issue will be resolved by OpenWeather soon.",
            503 => "OpenWeather returned Status Code 503: Service unavailable. OpenWeather is currently unavailable. Please try again later.",
            _ => "Unknown error caused by OpenWeather. Please try again later."
        };
}