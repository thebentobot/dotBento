using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Models;
using dotBento.Infrastructure.Services.Api;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Options;
using System.Net;

namespace dotBento.Bot.Tests.Commands.SharedCommands;

public sealed class WeatherCommandTests
{
    private static (WeatherCommand Command, TestDbFactory Factory) CreateCommand(HttpResponseMessage? weatherResponse = null)
    {
        var factory = new TestDbFactory();
        var command = new WeatherCommand(
            new WeatherService(factory),
            new WeatherApiService(new HttpClient(new StubHttpMessageHandler(weatherResponse ?? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(WeatherJson)
            }))),
            Options.Create(new BotEnvConfig { OpenWeatherApiKey = "weather-key" }));
        return (command, factory);
    }

    [Fact]
    public async Task GetWeatherAsync_ReturnsErrorWhenNoCitySavedOrProvided()
    {
        var (command, _) = CreateCommand();

        var response = await command.GetWeatherAsync(10, "tester", "https://example.com/avatar.png", null);

        Assert.Equal("Error: No city saved or provided", response.Embed.Build().Title);
    }

    [Fact]
    public async Task SaveWeatherAsync_SavesCity()
    {
        var (command, factory) = CreateCommand();

        var response = await command.SaveWeatherAsync(10, "copenhagen");

        Assert.Equal("Copenhagen was saved!", response.Embed.Build().Title);
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal("copenhagen", db.Weathers.Single().City);
    }

    [Fact]
    public async Task DeleteWeatherAsync_RemovesSavedCity()
    {
        var (command, factory) = CreateCommand();
        await command.SaveWeatherAsync(10, "copenhagen");

        var response = await command.DeleteWeatherAsync(10);

        Assert.Equal("Your saved city was successfully deleted!", response.Embed.Build().Title);
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Empty(db.Weathers);
    }

    [Fact]
    public async Task GetWeatherAsync_UsesProvidedCityAndFormatsWeather()
    {
        var (command, _) = CreateCommand();

        var response = await command.GetWeatherAsync(10, "tester", "avatar.png", "copenhagen");
        var embed = response.Embed.Build();

        Assert.Contains("Light rain", embed.Title);
        Assert.Contains("Copenhagen", embed.Title);
        Assert.Contains("mm the last hour", embed.Description);
        Assert.Contains(
            "🗺️ [See on Google Maps](https://www.google.com/maps/search/?api=1&query=55.6761,12.5683)",
            embed.Description);
        Assert.Equal("https://openweathermap.org/city/2618425", embed.Url);
        Assert.Equal("OpenWeather", embed.Author!.Value.Name);
    }

    [Fact]
    public async Task GetWeatherAsync_UsesSavedCityWhenNoCityProvided()
    {
        var (command, _) = CreateCommand();
        await command.SaveWeatherAsync(10, "copenhagen");

        var response = await command.GetWeatherAsync(10, "tester", "https://example.com/avatar.png", null);
        var embed = response.Embed.Build();

        Assert.Contains("Copenhagen", embed.Title);
        Assert.Equal("tester", embed.Author!.Value.Name);
    }

    [Fact]
    public async Task GetWeatherAsync_ReturnsErrorWhenApiFails()
    {
        var (command, _) = CreateCommand(new HttpResponseMessage(HttpStatusCode.NotFound));

        var response = await command.GetWeatherAsync(10, "tester", "avatar.png", "missing");

        Assert.Equal("Error", response.Embed.Build().Title);
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(response);
        }
    }

    private const string WeatherJson = """
    {
      "name": "Copenhagen",
      "id": 2618425,
      "cod": 200,
      "message": null,
      "weather": [
        { "id": 500, "main": "Rain", "description": "light rain", "icon": "10d" }
      ],
      "sys": { "country": "DK", "sunrise": 1780711200, "sunset": 1780772400, "id": 1, "type": 1 },
      "main": {
        "temp": 18.4,
        "feels_like": 17.8,
        "temp_min": 16.1,
        "temp_max": 20.2,
        "pressure": 1012,
        "humidity": 70
      },
      "dt": 1780732800,
      "timezone": 7200,
      "visibility": 8000,
      "base": "stations",
      "clouds": { "all": 75 },
      "wind": { "speed": 4.2, "deg": 45 },
      "coord": { "lon": 12.5683, "lat": 55.6761 },
      "rain": { "1h": 0.8, "3h": 1.4 },
      "snow": null
    }
    """;
}
