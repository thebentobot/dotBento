using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace dotBento.Infrastructure.Services;

public sealed class HoroscopeService(IDbContextFactory<BotDbContext> contextFactory)
{
    public async Task<Maybe<Horoscope>> GetHoroscopeAsync(long userId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return (await context.Horoscopes.SingleOrDefaultAsync(x => x.UserId == userId)).AsMaybe();
    }

    public async Task SaveHoroscopeAsync(long userId, string sign)
    {
        var normalizedSign = sign.Trim().ToLowerInvariant();
        await using var context = await contextFactory.CreateDbContextAsync();
        var horoscope = await context.Horoscopes.SingleOrDefaultAsync(x => x.UserId == userId);
        if (horoscope is null)
        {
            await context.Horoscopes.AddAsync(new Horoscope
            {
                UserId = userId,
                Sign = normalizedSign
            });
        }
        else
        {
            horoscope.Sign = normalizedSign;
        }

        await context.SaveChangesAsync();
    }

    public async Task DeleteHoroscopeAsync(long userId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var horoscope = await context.Horoscopes.SingleOrDefaultAsync(x => x.UserId == userId);
        if (horoscope is null)
            return;

        context.Horoscopes.Remove(horoscope);
        await context.SaveChangesAsync();
    }
}
