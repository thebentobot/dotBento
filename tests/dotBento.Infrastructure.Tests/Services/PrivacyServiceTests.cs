using Discord;
using Moq;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Tests.Services;

public class PrivacyServiceTests
{
    private static PrivacyService CreateService(InfrastructureTestDbFactory factory, IMemoryCache cache) =>
        new(factory, cache);

    [Fact]
    public async Task IsUserOptedOutAsync_ReflectsDatabaseAndCachesValue()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(factory, cache);

        Assert.False(await service.IsUserOptedOutAsync(10));

        await service.OptOutAndDeleteAsync(10);
        Assert.True(await service.IsUserOptedOutAsync(10));

        // The cached value is served even if the row is removed out-of-band.
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.UserOptOuts.RemoveRange(db.UserOptOuts.Where(o => o.UserId == 10));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        Assert.True(await service.IsUserOptedOutAsync(10));

        await service.RemoveOptOutAsync(10);
        Assert.False(await service.IsUserOptedOutAsync(10));
    }

    [Fact]
    public async Task OptOutAndDeleteAsync_AddsOptOutAndDeletesUserData_Idempotently()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(new User { UserId = 10, Username = "User", Discriminator = "0001", Level = 1, Xp = 0 });
            db.GuildMembers.Add(new GuildMember { GuildId = 100, UserId = 10, Level = 1, Xp = 0 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory, cache);

        await service.OptOutAndDeleteAsync(10);
        await service.OptOutAndDeleteAsync(10);

        await using var assertDb = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.True(await assertDb.UserOptOuts.AnyAsync(o => o.UserId == 10, TestContext.Current.CancellationToken));
        Assert.False(await assertDb.Users.AnyAsync(u => u.UserId == 10, TestContext.Current.CancellationToken));
        Assert.False(await assertDb.GuildMembers.AnyAsync(gm => gm.UserId == 10, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RemoveOptOutAsync_RemovesOptOutRow()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.UserOptOuts.Add(new UserOptOut { UserId = 10, OptedOutAt = DateTime.UtcNow });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory, cache);
        cache.Set("user-opt-out-10", true);

        await service.RemoveOptOutAsync(10);

        await using var assertDb = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.False(await assertDb.UserOptOuts.AnyAsync(o => o.UserId == 10, TestContext.Current.CancellationToken));
        Assert.False(await service.IsUserOptedOutAsync(10));
    }
    [Fact]
    public async Task OptOutAndDeleteAsync_InvalidatesPersonalDataCaches()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var distributed = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(new User { UserId = 10, Username = "User", Discriminator = "0001" });
            db.GuildMembers.Add(new GuildMember { GuildId = 100, UserId = 10 });
            db.Reminders.Add(new Reminder { Id = 20, UserId = 10, Reminder1 = "personal reminder" });
            db.Tags.Add(new Tag { TagId = 30, UserId = 10, GuildId = 100, Command = "personal", Content = "secret" });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        object[] keys = ["user-10", 10L, "guild-member-100-10", "reminder:20", "Tag_100_personal"];
        foreach (var key in keys)
            cache.Set(key, "personal data");
        await distributed.SetStringAsync("profile:10", "personal profile", TestContext.Current.CancellationToken);
        await distributed.SetStringAsync("user-hide-commands-10", "True", TestContext.Current.CancellationToken);
        await distributed.SetStringAsync("profile:11", "another user", TestContext.Current.CancellationToken);

        await new PrivacyService(factory, cache, distributed).OptOutAndDeleteAsync(10);

        foreach (var key in keys)
            Assert.False(cache.TryGetValue(key, out _));
        Assert.Null(await distributed.GetStringAsync("profile:10", TestContext.Current.CancellationToken));
        Assert.Null(await distributed.GetStringAsync("user-hide-commands-10", TestContext.Current.CancellationToken));
        Assert.Equal("another user", await distributed.GetStringAsync("profile:11", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OptOutAndDeleteAsync_WaitsForInFlightCreationAndPreventsRecreation()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var privacy = new PrivacyService(factory, cache);
        var users = new UserService(cache, factory, privacyService: privacy);
        Task deletion = Task.CompletedTask;
        var discordUser = new Mock<IUser>();
        discordUser.SetupGet(u => u.Id).Returns(10);
        discordUser.SetupGet(u => u.Discriminator).Returns("0001");
        discordUser.SetupGet(u => u.Username).Returns(() =>
        {
            // Creation has checked suppression and is preparing its INSERT.
            deletion = privacy.OptOutAndDeleteAsync(10);
            Assert.False(deletion.IsCompleted);
            return "User";
        });

        Assert.True((await users.CreateOrAddUserToCache(discordUser.Object)).IsSuccess);
        await deletion;
        // Even a stale negative opt-out cache cannot permit creation.
        cache.Set("user-opt-out-10", false);
        Assert.True((await users.CreateOrAddUserToCache(discordUser.Object)).IsFailure);
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Empty(db.Users);
        Assert.True(await db.UserOptOuts.AnyAsync(o => o.UserId == 10, TestContext.Current.CancellationToken));
        Assert.False(cache.TryGetValue("user-10", out _));
    }
}
