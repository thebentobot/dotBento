using System.Globalization;
using Discord;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Models.Weather;
using dotBento.Infrastructure.Services;
using dotBento.Infrastructure.Services.Api;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.SharedCommands;

public class WeatherCommand(
    WeatherService weatherService,
    WeatherApiService weatherApiService,
    IOptions<BotEnvConfig> config)
{
    public async Task<ResponseModel> GetWeatherAsync(long userId, string username, string userAvatar, string? userCity)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        string city;
        if (userCity is null)
        {
            var weather = await weatherService.GetWeatherAsync(userId);
            if (weather.HasNoValue)
            {
                embed.Embed
                    .WithColor(Color.Red)
                    .WithTitle("Error: No city saved or provided")
                    .WithDescription("You need to provide a city or save one with the weather save command\nFind the weather save command as a slash command or by trying help after calling the weather command.");
                return embed;
            }
            city = weather.Value.City;
        } else
        {
            city = userCity;
        }
        
        var weatherDataResult = await weatherApiService.GetWeatherForCity(city, config.Value.OpenWeatherApiKey);
        if (weatherDataResult.IsFailure)
        {
            embed.Embed
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription(weatherDataResult.Error);
            return embed;
        }
        var weatherData = weatherDataResult.Value;
        var currentWeather = weatherData.Weather.First();
        var cultureInfo = GetCultureInfoByCountryCode(weatherData.Sys.Country);
        
        var openWeatherColour = OpenWeatherColour;
        const string openWeatherLogo = "https://pbs.twimg.com/profile_images/1173919481082580992/f95OeyEW_400x400.jpg";
        
        var openWeatherAuthor = new EmbedAuthorBuilder()
            .WithName(userCity != null ? "OpenWeather" : username)
            .WithIconUrl(userCity != null ? openWeatherLogo : userAvatar);

        var lastUpdated = DateTimeOffset.FromUnixTimeSeconds(weatherData.Dt)
            .AddSeconds(weatherData.Timezone)
            .ToString(cultureInfo.DateTimeFormat.ShortTimePattern);
        var openWeatherFooter = new EmbedFooterBuilder()
            .WithText($"Last updated at {lastUpdated} {GetFlagEmoji(weatherData.Sys.Country)} time")
            .WithIconUrl(userCity != null ? null : openWeatherLogo);

        var title = $"{currentWeather.Description.CapitalizeFirstLetter()} {WeatherEmote(currentWeather.Id)} in {weatherData.Name}, {GetFlagEmoji(weatherData.Sys.Country)} {GetCountryFromEnglishName(cultureInfo)}";
        var rainOrSnow = string.Join("\n", IfRainOrSnow(weatherData.Rain, weatherData.Snow));
        var description = CreateWeatherDescription(weatherData, cultureInfo);

        embed.Embed
            .WithAuthor(openWeatherAuthor)
            .WithFooter(openWeatherFooter)
            .WithColor(openWeatherColour)
            .WithTitle(title)
            .WithUrl($"https://openweathermap.org/city/{weatherData.Id}")
            .WithThumbnailUrl($"https://openweathermap.org/img/w/{currentWeather.Icon}.png")
            .WithDescription(rainOrSnow + description)
            .WithTimestamp(DateTimeOffset.FromUnixTimeSeconds(weatherData.Dt));
        
        return embed;
    }
    
    public async Task<ResponseModel> SaveWeatherAsync(long userId, string city)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        await weatherService.SaveWeatherAsync(userId, city);
        embed.Embed
            .WithColor(OpenWeatherColour)
            .WithTitle($"{city.CapitalizeFirstLetter()} was saved!")
            .WithDescription("You can now use the weather command without any input, if you want to instantly check the weather at your saved location \ud83d\ude0e");
        return embed;
    }
    
    public async Task<ResponseModel> DeleteWeatherAsync(long userId)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        await weatherService.DeleteWeatherAsync(userId);
        embed.Embed
            .WithColor(OpenWeatherColour)
            .WithTitle("Your saved city was successfully deleted!");
        return embed;
    }
    
    private static Color OpenWeatherColour => new(0xEB6E4B);

    private static string CreateWeatherDescription(OpenWeatherApiObject weatherData, CultureInfo cultureInfo) =>
        string.Format(
            "🌡 {0}°C ({1}°F), feels like {2}°C ({3}°F)\n" +
            "⚖️ Min. {4}°C ({5}°F), Max. {6}°C ({7}°F)\n" +
            "☁️ {8}% Cloudiness 💦 {9}% Humidity\n" +
            "💨{11} {10} m/s \ud83c\udf01 {12}% Fog\n\n" +
            "🕒 {13} {14}\n" +
            "🌅 {15}\n" +
            "🌇 {16}",
            Math.Round(weatherData.Main.Temp),
            ConvertCelsiusToFahrenheit(weatherData.Main.Temp),
            Math.Round(weatherData.Main.FeelsLike),
            ConvertCelsiusToFahrenheit(weatherData.Main.FeelsLike),
            Math.Round(weatherData.Main.TempMin),
            ConvertCelsiusToFahrenheit(weatherData.Main.TempMin),
            Math.Round(weatherData.Main.TempMax),
            ConvertCelsiusToFahrenheit(weatherData.Main.TempMax),
            weatherData.Clouds.All,
            weatherData.Main.Humidity,
            weatherData.Wind.Speed,
            WindDirection(weatherData.Wind.Deg),
            CalculateFogginess(weatherData.Visibility).ToString(cultureInfo),
            DateTimeOffset.UtcNow.AddSeconds(weatherData.Timezone).ToString(cultureInfo.DateTimeFormat.ShortTimePattern),
            GetFlagEmoji(weatherData.Sys.Country),
            DateTimeOffset.FromUnixTimeSeconds(weatherData.Sys.Sunrise).AddSeconds(weatherData.Timezone).ToString(cultureInfo.DateTimeFormat.ShortTimePattern),
            DateTimeOffset.FromUnixTimeSeconds(weatherData.Sys.Sunset).AddSeconds(weatherData.Timezone).ToString(cultureInfo.DateTimeFormat.ShortTimePattern)
        );

    private static CultureInfo GetCultureInfoByCountryCode(string countryCode)
    {
        var cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
        var specificCulture = cultures.FirstOrDefault(culture => culture.Name.EndsWith(countryCode));
        return specificCulture?? new CultureInfo("en-US");
    }
    
    private static string GetFlagEmoji(string countryCode)
    {
        const int regionalIndicatorSymbolA = 0x1F1E6;
        var flag = string.Concat(countryCode
            .ToUpper()
            .Select(c => c - 'A' + regionalIndicatorSymbolA)
            .Select(char.ConvertFromUtf32));
        return flag;
    }
    
    private static string WeatherEmote(int weather)
    {
        return weather switch
        {
            >= 210 and <= 221 => "⛈️",
            >= 200 and <= 202 => "🌩️",
            >= 230 and <= 232 => "⛈️",
            >= 300 and <= 321 => "🌧️",
            >= 500 and <= 504 => "🌦️",
            511 => "🌨️",
            >= 520 and <= 531 => "🌧️",
            >= 600 and <= 622 => "❄️",
            >= 701 and <= 781 => "🌫️",
            800 => "☀️",
            801 => "⛅",
            >= 802 and <= 804 => "☁️",
            _ => string.Empty
        };
    }

    private static string GetCountryFromEnglishName(CultureInfo culture)
    {
        var englishName = culture.EnglishName; 
        var firstParenthesis = englishName.IndexOf('(');
        var lastParenthesis = englishName.IndexOf(')');

        if(firstParenthesis >= 0 && lastParenthesis > firstParenthesis)
        {
            return englishName.Substring(firstParenthesis + 1, lastParenthesis - firstParenthesis - 1);
        }
        return "";
    }
    
    private static int ConvertCelsiusToFahrenheit(double celsius) 
    {
        return (int) Math.Round(celsius * 9 / 5 + 32);
    }
    
    private static string WindDirection(double degree)
    {
        switch (degree)
        {
            case 90:
                return "⬆️";
            case 270:
                return "⬇️";
            case 180:
                return "⬅️";
            case 360:
            case 0:
                return "➡️";
            case > 0 and < 90:
                return "↗️";
            case > 270 and < 360:
                return "↘️";
            case > 180 and < 270:
                return "↙️";
            case > 90 and < 180:
                return "↖️";
            default:
                return string.Empty;
        }
    }

    private static int CalculateFogginess(int visibilityInMeters)
    {
        const int maxVisibilityInMeters = 10000;
        var visibilityPercentage = ((double)visibilityInMeters / maxVisibilityInMeters) * 100;
        var fogginessPercentage = 100 - (int)Math.Round(visibilityPercentage);
    
        return fogginessPercentage;
    }
    
    private static IEnumerable<string> IfRainOrSnow(OpenWeatherApiRainObject? rain, OpenWeatherApiSnowObject? snow)
    {
        var results = new List<string>();
    
        if (rain is not null)
        {
            var rainString = "🌧️ ";
            if (rain.OneHour != null) rainString += $"{rain.OneHour} mm the last hour";
            if (rain is { OneHour: not null, ThreeHours: not null }) rainString += ", ";
            if (rain.ThreeHours != null) rainString += $"{rain.ThreeHours} mm last 3 hours";
            rainString += ".\n";
            results.Add(rainString);
        }
    
        if (snow is not null)
        {
            var snowString = "🌨️ ";
            if (snow.OneHour != null) snowString += $"{snow.OneHour} mm the last hour";
            if (snow is { OneHour: not null, ThreeHours: not null }) snowString += ", ";
            if (snow.ThreeHours != null) snowString += $"{snow.ThreeHours} mm last 3 hours";
            snowString += ".\n";
            results.Add(snowString);
        }

        return results.ToArray();
    }
}