using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Bot.Services;

public class WeatherService(IMemoryCache cache,
    IDbContextFactory<BotDbContext> contextFactory)
{
    public async Task<Maybe<Weather>> GetWeatherAsync(long userId)
    {
        var context = await contextFactory.CreateDbContextAsync();
        var weather = context.Weathers.SingleOrDefault(x => x.UserId == userId).AsMaybe();

        return weather;
    }
    
    public async Task SaveWeatherAsync(long userId, string city)
    {
        var context = await contextFactory.CreateDbContextAsync();
        var weather = context.Weathers.SingleOrDefault(x => x.UserId == userId);
        if (weather is null)
        {
            weather = new Weather
            {
                UserId = userId,
                City = city
            };
            await context.Weathers.AddAsync(weather);
        }
        else
        {
            weather.City = city;
        }
        await context.SaveChangesAsync();
    }
    
    public async Task DeleteWeatherAsync(long userId)
    {
        var context = await contextFactory.CreateDbContextAsync();
        var weather = context.Weathers.SingleOrDefault(x => x.UserId == userId);
        if (weather is null) return;
        context.Weathers.Remove(weather);
        await context.SaveChangesAsync();
    }
}