using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace dotBento.Infrastructure.Tests.Services;

public class SettingsServicesTests
{
    [Fact]
    public async Task GuildSettingService_CreatesUpdatesAndCachesLeaderboardVisibility()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new GuildSettingService(factory, cache);

        var created = await service.GetOrCreateGuildSettingAsync(10);
        var initiallyPublic = await service.IsLeaderboardPublicAsync(10);
        var updated = await service.UpdateLeaderboardPublicAsync(10, true);
        var publicAfterUpdate = await service.IsLeaderboardPublicAsync(10);

        Assert.False(created.LeaderboardPublic);
        Assert.False(initiallyPublic);
        Assert.True(updated.LeaderboardPublic);
        Assert.True(publicAfterUpdate);
    }

    [Fact]
    public async Task GuildSettingService_ReturnsExistingCreatesOnUpdateAndUsesCache()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.GuildSettings.Add(new GuildSetting
            {
                GuildId = 10,
                LeaderboardPublic = true
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = new GuildSettingService(factory, cache);

        var existing = await service.GetOrCreateGuildSettingAsync(10);
        cache.Set("guild-setting-10", false);
        var cached = await service.IsLeaderboardPublicAsync(10);
        var createdByUpdate = await service.UpdateLeaderboardPublicAsync(20, true);

        Assert.True(existing.LeaderboardPublic);
        Assert.False(cached);
        Assert.Equal(20, createdByUpdate.GuildId);
        Assert.True(createdByUpdate.LeaderboardPublic);
    }

    [Fact]
    public async Task UserSettingService_CreatesUpdatesCachesAndListsHiddenLeaderboardUsers()
    {
        var factory = new InfrastructureTestDbFactory();
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var service = new UserSettingService(factory, cache);

        var created = await service.GetOrCreateUserSettingAsync(10);
        var hideBeforeUpdate = await service.ShouldHideCommandsAsync(10);
        var updated = await service.UpdateUserSettingAsync(10, setting =>
        {
            setting.HideSlashCommandCalls = true;
            setting.ShowOnGlobalLeaderboard = false;
        });
        var hideAfterUpdate = await service.ShouldHideCommandsAsync(10);
        var hiddenUsers = await service.GetHiddenGlobalLeaderboardUserIdsAsync();

        Assert.False(created.HideSlashCommandCalls);
        Assert.True(created.ShowOnGlobalLeaderboard);
        Assert.False(hideBeforeUpdate);
        Assert.True(updated.HideSlashCommandCalls);
        Assert.False(updated.ShowOnGlobalLeaderboard);
        Assert.True(hideAfterUpdate);
        Assert.Contains(10, hiddenUsers);
    }

    [Fact]
    public async Task UserSettingService_ReturnsExistingSettings()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.UserSettings.Add(new UserSetting
            {
                UserId = 20,
                HideSlashCommandCalls = true,
                ShowOnGlobalLeaderboard = false
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = new UserSettingService(
            factory,
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));

        var setting = await service.GetOrCreateUserSettingAsync(20);

        Assert.True(setting.HideSlashCommandCalls);
        Assert.False(setting.ShowOnGlobalLeaderboard);
    }

    [Fact]
    public async Task UserSettingService_ShouldHideCommandsAsync_UsesCachedValue()
    {
        var factory = new InfrastructureTestDbFactory();
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        await cache.SetStringAsync("user-hide-commands-10", "True", TestContext.Current.CancellationToken);
        var service = new UserSettingService(factory, cache);

        var shouldHide = await service.ShouldHideCommandsAsync(10);

        Assert.True(shouldHide);
    }
}
