using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;

namespace dotBento.Infrastructure.Services;

/// <summary>
/// Owns the persistent data-deletion opt-out ("suppression list"). An opted-out user keeps a single
/// row in <c>userOptOut</c> so Bento can honour the request; all other data is deleted.
/// </summary>
public sealed class PrivacyService(IDbContextFactory<BotDbContext> contextFactory, IMemoryCache cache,
    IDistributedCache? distributedCache = null)
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
        await using var userLock = await UserDataLock.AcquireAsync(db, userId);

        if (!await db.UserOptOuts.AsQueryable().AnyAsync(o => o.UserId == userId))
            db.UserOptOuts.Add(new UserOptOut { UserId = userId, OptedOutAt = DateTime.UtcNow });

        // Remove guild member rows explicitly in case the user row is already gone.
        var guildMembers = await db.GuildMembers
            .AsQueryable()
            .Where(gm => gm.UserId == userId)
            .ToListAsync();
        if (guildMembers.Count > 0)
            db.GuildMembers.RemoveRange(guildMembers);

        var reminderIds = await db.Reminders.Where(r => r.UserId == userId).Select(r => r.Id).ToListAsync();
        var tags = await db.Tags.Where(t => t.UserId == userId)
            .Select(t => new { t.GuildId, t.Command }).ToListAsync();

        var user = await db.Users
            .AsQueryable()
            .FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is not null)
            db.Users.Remove(user);

        await db.SaveChangesAsync();

        await userLock.CommitAsync();

        cache.Set(OptOutCacheKey(userId), true, OptOutCacheDuration);
        // Mirrors UserService's user cache key so a stale entry cannot outlive the deletion.
        cache.Remove($"user-{userId}");
        cache.Remove(userId); // Bento and supporter caches use the user ID directly.
        foreach (var member in guildMembers)
            cache.Remove($"guild-member-{member.GuildId}-{userId}");
        foreach (var reminderId in reminderIds)
            cache.Remove($"reminder:{reminderId}");
        foreach (var tag in tags)
            cache.Remove($"Tag_{tag.GuildId}_{tag.Command}");
        if (distributedCache is not null)
        {
            await distributedCache.RemoveAsync($"profile:{userId}");
            await distributedCache.RemoveAsync($"user-hide-commands-{userId}");
        }
    }

    public async Task RemoveOptOutAsync(long userId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        await using var userLock = await UserDataLock.AcquireAsync(db, userId);
        var optOut = await db.UserOptOuts
            .AsQueryable()
            .FirstOrDefaultAsync(o => o.UserId == userId);

        if (optOut is not null)
        {
            db.UserOptOuts.Remove(optOut);
            await db.SaveChangesAsync();
        }

        await userLock.CommitAsync();
        cache.Set(OptOutCacheKey(userId), false, OptOutCacheDuration);
    }

    private static string OptOutCacheKey(long userId) => $"user-opt-out-{userId}";
}
