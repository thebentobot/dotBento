using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Services;

public sealed class GuildSettingService(IDbContextFactory<BotDbContext> contextFactory, IMemoryCache cache)
{
    private static readonly Lock NonRelationalPermissionLocksGate = new();
    private static readonly Dictionary<long, PermissionLockEntry> NonRelationalPermissionLocks = [];

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

    public async Task<CommandPermissions> GetCommandPermissionsAsync(long guildId)
    {
        var cacheKey = CommandPermissionsCacheKey(guildId);
        if (cache.TryGetValue(cacheKey, out CommandPermissions? cached) && cached is not null)
            return cached;

        await using var db = await contextFactory.CreateDbContextAsync();
        var settings = await db.GuildSettings
            .Where(s => s.GuildId == guildId)
            .Select(s => new { s.DisabledCommands, s.AdminOnlyCommands })
            .FirstOrDefaultAsync();

        var permissions = settings is not null
            ? new CommandPermissions(settings.DisabledCommands, settings.AdminOnlyCommands)
            : new CommandPermissions([], []);

        cache.Set(cacheKey, permissions, TimeSpan.FromMinutes(5));
        return permissions;
    }

    public async Task SetCommandDisabledAsync(long guildId, string commandId, bool disabled)
    {
        await MutateCommandPermissionsAsync(guildId, setting =>
        {
            setting.DisabledCommands = UpdateCommandList(setting.DisabledCommands, commandId, disabled);
            if (disabled)
                setting.AdminOnlyCommands = UpdateCommandList(setting.AdminOnlyCommands, commandId, false);
        });
    }

    public async Task SetCommandAdminOnlyAsync(long guildId, string commandId, bool adminOnly)
    {
        await MutateCommandPermissionsAsync(guildId, setting =>
        {
            setting.AdminOnlyCommands = UpdateCommandList(setting.AdminOnlyCommands, commandId, adminOnly);
            if (adminOnly)
                setting.DisabledCommands = UpdateCommandList(setting.DisabledCommands, commandId, false);
        });
    }

    private async Task MutateCommandPermissionsAsync(long guildId, Action<GuildSetting> mutation)
    {
        await using var db = await contextFactory.CreateDbContextAsync();

        if (db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var executionStrategy = db.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var retryDb = await contextFactory.CreateDbContextAsync();
                await using var transaction = await retryDb.Database.BeginTransactionAsync();

                await retryDb.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO "guildSetting" ("guildID")
                    VALUES ({guildId})
                    ON CONFLICT ("guildID") DO NOTHING
                    """);

                var setting = await retryDb.GuildSettings
                    .FromSqlInterpolated($"""
                        SELECT *
                        FROM "guildSetting"
                        WHERE "guildID" = {guildId}
                        FOR UPDATE
                        """)
                    .SingleAsync();

                mutation(setting);
                await retryDb.SaveChangesAsync();
                await transaction.CommitAsync();
            });
        }
        else
        {
            using var permissionLock = await AcquireNonRelationalPermissionLockAsync(guildId);
            var setting = await db.GuildSettings.FirstOrDefaultAsync(s => s.GuildId == guildId);
            if (setting is null)
            {
                setting = new GuildSetting { GuildId = guildId };
                db.GuildSettings.Add(setting);
            }

            mutation(setting);
            await db.SaveChangesAsync();
        }

        InvalidateCommandPermissionsCache(guildId);
    }

    private static string[] UpdateCommandList(
        IEnumerable<string> commands,
        string commandId,
        bool shouldContain)
    {
        var updated = commands.ToList();
        if (shouldContain)
        {
            if (!updated.Contains(commandId, StringComparer.OrdinalIgnoreCase))
                updated.Add(commandId);
        }
        else
        {
            updated.RemoveAll(command => command.Equals(commandId, StringComparison.OrdinalIgnoreCase));
        }

        return [..updated];
    }

    private static string GuildSettingCacheKey(long guildId) => $"guild-setting-{guildId}";

    private static string CommandPermissionsCacheKey(long guildId) => $"guild-command-permissions-{guildId}";

    private void InvalidateCommandPermissionsCache(long guildId)
    {
        cache.Remove(CommandPermissionsCacheKey(guildId));
    }

    private static async Task<IDisposable> AcquireNonRelationalPermissionLockAsync(long guildId)
    {
        PermissionLockEntry entry;
        lock (NonRelationalPermissionLocksGate)
        {
            if (!NonRelationalPermissionLocks.TryGetValue(guildId, out entry!))
            {
                entry = new PermissionLockEntry();
                NonRelationalPermissionLocks.Add(guildId, entry);
            }

            entry.ReferenceCount++;
        }

        try
        {
            await entry.Semaphore.WaitAsync();
            return new PermissionLockLease(guildId, entry);
        }
        catch
        {
            ReleaseNonRelationalPermissionLock(guildId, entry, releaseSemaphore: false);
            throw;
        }
    }

    private static void ReleaseNonRelationalPermissionLock(
        long guildId,
        PermissionLockEntry entry,
        bool releaseSemaphore)
    {
        if (releaseSemaphore)
            entry.Semaphore.Release();

        var disposeSemaphore = false;
        lock (NonRelationalPermissionLocksGate)
        {
            entry.ReferenceCount--;
            if (entry.ReferenceCount == 0)
            {
                NonRelationalPermissionLocks.Remove(guildId);
                disposeSemaphore = true;
            }
        }

        if (disposeSemaphore)
            entry.Semaphore.Dispose();
    }

    internal static bool HasNonRelationalPermissionLock(long guildId)
    {
        lock (NonRelationalPermissionLocksGate)
            return NonRelationalPermissionLocks.ContainsKey(guildId);
    }

    private sealed class PermissionLockEntry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int ReferenceCount { get; set; }
    }

    private sealed class PermissionLockLease(long guildId, PermissionLockEntry entry) : IDisposable
    {
        private PermissionLockEntry? _entry = entry;

        public void Dispose()
        {
            var entryToRelease = Interlocked.Exchange(ref _entry, null);
            if (entryToRelease is not null)
                ReleaseNonRelationalPermissionLock(guildId, entryToRelease, releaseSemaphore: true);
        }
    }
}

public sealed record CommandPermissions(string[] Disabled, string[] AdminOnly);
