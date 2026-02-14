using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Services;

public sealed class GuildSettingService(IDbContextFactory<BotDbContext> contextFactory, IMemoryCache cache)
{
    public async Task<GuildSetting> GetOrCreateGuildSettingAsync(long guildId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var setting = await db.GuildSettings
            .FirstOrDefaultAsync(s => s.GuildId == guildId);

        if (setting is not null)
            return setting;

        setting = new GuildSetting
        {
            GuildId = guildId,
            LeaderboardPublic = false
        };
        db.GuildSettings.Add(setting);
        await db.SaveChangesAsync();
        return setting;
    }

    public async Task<GuildSetting> UpdateLeaderboardPublicAsync(long guildId, bool isPublic)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var setting = await db.GuildSettings
            .FirstOrDefaultAsync(s => s.GuildId == guildId);

        if (setting is null)
        {
            setting = new GuildSetting
            {
                GuildId = guildId,
                LeaderboardPublic = isPublic
            };
            db.GuildSettings.Add(setting);
        }
        else
        {
            setting.LeaderboardPublic = isPublic;
        }

        await db.SaveChangesAsync();
        cache.Remove(GuildSettingCacheKey(guildId));
        return setting;
    }

    public async Task<bool> IsLeaderboardPublicAsync(long guildId)
    {
        var cacheKey = GuildSettingCacheKey(guildId);
        if (cache.TryGetValue(cacheKey, out bool cachedValue))
            return cachedValue;

        await using var db = await contextFactory.CreateDbContextAsync();
        var isPublic = await db.GuildSettings
            .Where(s => s.GuildId == guildId)
            .Select(s => s.LeaderboardPublic)
            .FirstOrDefaultAsync();

        cache.Set(cacheKey, isPublic, TimeSpan.FromMinutes(5));
        return isPublic;
    }

    private static string GuildSettingCacheKey(long guildId) => $"guild-setting-{guildId}";
}
