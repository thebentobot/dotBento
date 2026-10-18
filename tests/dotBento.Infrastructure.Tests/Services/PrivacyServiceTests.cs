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
}
