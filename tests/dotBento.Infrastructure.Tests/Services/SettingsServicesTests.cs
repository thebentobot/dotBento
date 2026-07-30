using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
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
    public async Task GuildSettingService_DisablingCommand_ReplacesAdminOnlyAndInvalidatesCache()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        await SeedGuildSettingAsync(factory, new GuildSetting
        {
            GuildId = 10,
            AdminOnlyCommands = ["tag:create"]
        });
        var service = new GuildSettingService(factory, cache);
        var cachedBeforeMutation = await service.GetCommandPermissionsAsync(10);

        await service.SetCommandDisabledAsync(10, "tag:create", true);
        var afterMutation = await service.GetCommandPermissionsAsync(10);

        Assert.Equal(["tag:create"], cachedBeforeMutation.AdminOnly);
        Assert.Empty(cachedBeforeMutation.Disabled);
        Assert.Equal(["tag:create"], afterMutation.Disabled);
        Assert.Empty(afterMutation.AdminOnly);
    }

    [Fact]
    public async Task GuildSettingService_AddingAdminOnly_ReplacesDisabledAndInvalidatesCache()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        await SeedGuildSettingAsync(factory, new GuildSetting
        {
            GuildId = 10,
            DisabledCommands = ["game:rps"]
        });
        var service = new GuildSettingService(factory, cache);
        var cachedBeforeMutation = await service.GetCommandPermissionsAsync(10);

        await service.SetCommandAdminOnlyAsync(10, "game:rps", true);
        var afterMutation = await service.GetCommandPermissionsAsync(10);

        Assert.Equal(["game:rps"], cachedBeforeMutation.Disabled);
        Assert.Empty(cachedBeforeMutation.AdminOnly);
        Assert.Empty(afterMutation.Disabled);
        Assert.Equal(["game:rps"], afterMutation.AdminOnly);
    }

    [Fact]
    public async Task GuildSettingService_RemovesStaleStoredIdsAndInvalidatesCache()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        await SeedGuildSettingAsync(factory, new GuildSetting
        {
            GuildId = 10,
            DisabledCommands = ["removed:disabled", "server:settings"],
            AdminOnlyCommands = ["removed:admin"]
        });
        var service = new GuildSettingService(factory, cache);
        _ = await service.GetCommandPermissionsAsync(10);

        await service.SetCommandDisabledAsync(10, "removed:disabled", false);
        var afterDisabledRemoval = await service.GetCommandPermissionsAsync(10);
        await service.SetCommandAdminOnlyAsync(10, "removed:admin", false);
        var afterAdminRemoval = await service.GetCommandPermissionsAsync(10);
        await service.SetCommandDisabledAsync(10, "server:settings", false);
        var afterProtectedRemoval = await service.GetCommandPermissionsAsync(10);

        Assert.Equal(["server:settings"], afterDisabledRemoval.Disabled);
        Assert.Equal(["removed:admin"], afterDisabledRemoval.AdminOnly);
        Assert.Equal(["server:settings"], afterAdminRemoval.Disabled);
        Assert.Empty(afterAdminRemoval.AdminOnly);
        Assert.Empty(afterProtectedRemoval.Disabled);
        Assert.Empty(afterProtectedRemoval.AdminOnly);
    }

    [Fact]
    public async Task GuildSettingService_ConcurrentPermissionUpdates_PreserveBothChanges()
    {
        const long guildId = 10;
        using var readBarrier = new ConcurrentGuildSettingReadBarrier();
        var factory = new ConcurrentPermissionTestDbFactory(readBarrier);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        await SeedGuildSettingAsync(factory, new GuildSetting
        {
            GuildId = guildId,
            DisabledCommands = ["existing:disabled"]
        });
        var service = new GuildSettingService(factory, cache);
        using var ready = new CountdownEvent(2);
        using var start = new ManualResetEventSlim(false);

        var disableTask = RunConcurrentUpdateAsync(
            () => service.SetCommandDisabledAsync(guildId, "new:disabled-a", true));
        var secondDisableTask = RunConcurrentUpdateAsync(
            () => service.SetCommandDisabledAsync(guildId, "new:disabled-b", true));

        Assert.True(ready.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        start.Set();
        await Task.WhenAll(disableTask, secondDisableTask);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var persisted = await db.GuildSettings
            .AsNoTracking()
            .SingleAsync(s => s.GuildId == guildId, TestContext.Current.CancellationToken);
        var cached = await service.GetCommandPermissionsAsync(guildId);

        Assert.Equal(
            ["existing:disabled", "new:disabled-a", "new:disabled-b"],
            persisted.DisabledCommands.Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(3, persisted.DisabledCommands.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Empty(persisted.AdminOnlyCommands);
        Assert.Equal(
            persisted.DisabledCommands.Order(StringComparer.Ordinal),
            cached.Disabled.Order(StringComparer.Ordinal));
        Assert.Empty(cached.AdminOnly);

        Task RunConcurrentUpdateAsync(Func<Task> update) =>
            Task.Run(async () =>
            {
                ready.Signal();
                start.Wait(TestContext.Current.CancellationToken);
                await update();
            }, TestContext.Current.CancellationToken);
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

    private static async Task SeedGuildSettingAsync(
        IDbContextFactory<BotDbContext> factory,
        GuildSetting setting)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        db.GuildSettings.Add(setting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private sealed class ConcurrentPermissionTestDbFactory(
        IMaterializationInterceptor materializationInterceptor) : IDbContextFactory<BotDbContext>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly InMemoryDatabaseRoot _databaseRoot = new();
        private readonly IConfiguration _configuration = new ConfigurationBuilder().Build();

        public BotDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<BotDbContext>()
                .UseInMemoryDatabase(_databaseName, _databaseRoot)
                .AddInterceptors(materializationInterceptor)
                .Options;

            return new BotDbContext(_configuration, options);
        }

        public Task<BotDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    private sealed class ConcurrentGuildSettingReadBarrier : IMaterializationInterceptor, IDisposable
    {
        private readonly CountdownEvent _reads = new(2);

        public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
        {
            if (entity is not GuildSetting || _reads.CurrentCount == 0)
                return entity;

            _reads.Signal();
            _reads.Wait(TimeSpan.FromMilliseconds(250));
            return entity;
        }

        public void Dispose() => _reads.Dispose();
    }
}
