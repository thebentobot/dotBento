using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Services;

/// <summary>
/// Owns the persistent data-deletion opt-out ("suppression list"). An opted-out user keeps a single
/// row in <c>userOptOut</c> so Bento can honour the request; all other data is deleted.
/// </summary>
public sealed class PrivacyService(IDbContextFactory<BotDbContext> contextFactory, IMemoryCache cache)
{
    private static readonly TimeSpan OptOutCacheDuration = TimeSpan.FromMinutes(5);

    public async Task<bool> IsUserOptedOutAsync(long userId)
    {
        if (cache.TryGetValue(OptOutCacheKey(userId), out bool optedOut))
            return optedOut;

        await using var db = await contextFactory.CreateDbContextAsync();
        optedOut = await db.UserOptOuts
            .AsQueryable()
            .AnyAsync(o => o.UserId == userId);

        cache.Set(OptOutCacheKey(userId), optedOut, OptOutCacheDuration);
        return optedOut;
    }

    /// <summary>
    /// Records the opt-out and deletes every piece of data Bento stores for the user. The opt-out row
    /// is written in the same transaction as the deletion so the data can never be re-created again.
    /// </summary>
    public async Task OptOutAndDeleteAsync(long userId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();

        if (!await db.UserOptOuts.AsQueryable().AnyAsync(o => o.UserId == userId))
            db.UserOptOuts.Add(new UserOptOut { UserId = userId, OptedOutAt = DateTime.UtcNow });

        // Remove guild member rows explicitly in case the user row is already gone.
        var guildMembers = await db.GuildMembers
            .AsQueryable()
            .Where(gm => gm.UserId == userId)
            .ToListAsync();
        if (guildMembers.Count > 0)
            db.GuildMembers.RemoveRange(guildMembers);

        var user = await db.Users
            .AsQueryable()
            .FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is not null)
            db.Users.Remove(user);

        await db.SaveChangesAsync();

        cache.Set(OptOutCacheKey(userId), true, OptOutCacheDuration);
        // Mirrors UserService's user cache key so a stale entry cannot outlive the deletion.
        cache.Remove($"user-{userId}");
    }

    public async Task RemoveOptOutAsync(long userId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var optOut = await db.UserOptOuts
            .AsQueryable()
            .FirstOrDefaultAsync(o => o.UserId == userId);

        if (optOut is not null)
        {
            db.UserOptOuts.Remove(optOut);
            await db.SaveChangesAsync();
        }

        cache.Set(OptOutCacheKey(userId), false, OptOutCacheDuration);
    }

    private static string OptOutCacheKey(long userId) => $"user-opt-out-{userId}";
}
