using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Services;

public class SupporterService(IDbContextFactory<BotDbContext> contextFactory,
    IMemoryCache cache)
{
    public async Task<int> GetActiveSupporterCountAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Patreons
            .AsQueryable()
            .CountAsync();
    }
    
    public async Task<Maybe<Patreon>> GetPatreonAsync(long userId)
    {
        if (cache.TryGetValue<Patreon>(userId, out var patreon) && patreon != null)
        {
            return patreon.AsMaybe();
        }

        await using var db = await contextFactory.CreateDbContextAsync();
        patreon = await db.Patreons.FirstOrDefaultAsync(x => x.UserId == userId);
        if (patreon == null)
        {
            return Maybe<Patreon>.None;
        }

        cache.Set(userId, patreon, TimeSpan.FromMinutes(5));
        return patreon.AsMaybe();
    }
}