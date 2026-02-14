using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace dotBento.Infrastructure.Services;

public sealed class UserSettingService(IDbContextFactory<BotDbContext> contextFactory, IDistributedCache distributedCache)
{
    public async Task<UserSetting> GetOrCreateUserSettingAsync(long userId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var setting = await db.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (setting is not null)
            return setting;

        setting = new UserSetting
        {
            UserId = userId,
            HideSlashCommandCalls = false,
            ShowOnGlobalLeaderboard = true
        };
        db.UserSettings.Add(setting);
        await db.SaveChangesAsync();
        return setting;
    }

    public async Task<UserSetting> UpdateUserSettingAsync(long userId, Action<UserSetting> update)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var setting = await db.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (setting is null)
        {
            setting = new UserSetting
            {
                UserId = userId,
                HideSlashCommandCalls = false,
                ShowOnGlobalLeaderboard = true
            };
            db.UserSettings.Add(setting);
        }

        update(setting);
        await db.SaveChangesAsync();
        await InvalidateUserSettingCacheAsync(userId);
        return setting;
    }

    public async Task<bool> ShouldHideCommandsAsync(long userId)
    {
        var cacheKey = UserHideCommandsCacheKey(userId);
        var cached = await distributedCache.GetStringAsync(cacheKey);
        if (cached is not null)
            return bool.Parse(cached);

        await using var db = await contextFactory.CreateDbContextAsync();
        var hideCommands = await db.UserSettings
            .Where(s => s.UserId == userId)
            .Select(s => s.HideSlashCommandCalls)
            .FirstOrDefaultAsync();

        await distributedCache.SetStringAsync(cacheKey, hideCommands.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });

        return hideCommands;
    }

    public async Task<HashSet<long>> GetHiddenGlobalLeaderboardUserIdsAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var userIds = await db.UserSettings
            .Where(s => !s.ShowOnGlobalLeaderboard)
            .Select(s => s.UserId)
            .ToListAsync();

        return userIds.ToHashSet();
    }

    private async Task InvalidateUserSettingCacheAsync(long userId)
    {
        await distributedCache.RemoveAsync(UserHideCommandsCacheKey(userId));
    }

    private static string UserHideCommandsCacheKey(long userId) => $"user-hide-commands-{userId}";
}
