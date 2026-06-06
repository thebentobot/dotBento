using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Tests.Services;

public class BentoServiceTests
{
    private static BentoService CreateService(
        InfrastructureTestDbFactory factory,
        IMemoryCache? cache = null) =>
        new(cache ?? new MemoryCache(new MemoryCacheOptions()), factory);

    [Fact]
    public async Task FindOrCreateBentoAsync_WhenMissing_CreatesWithAmount()
    {
        var factory = new InfrastructureTestDbFactory();
        var service = CreateService(factory);

        var bento = await service.FindOrCreateBentoAsync(10, 4);

        Assert.Equal(10, bento.UserId);
        Assert.Equal(4, bento.Bento1);
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, await db.Bentos.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindOrCreateBentoAsync_WhenCached_ReturnsCachedValue()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set(10L, new Bento { UserId = 10, Bento1 = 99 });
        var service = CreateService(factory, cache);

        var bento = await service.FindOrCreateBentoAsync(10, 4);

        Assert.Equal(99, bento.Bento1);
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Empty(db.Bentos);
    }

    [Fact]
    public async Task FindBentoAsync_ReturnsNoneWhenMissing_AndCachesWhenFound()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(factory, cache);

        var missing = await service.FindBentoAsync(10);
        Assert.True(missing.HasNoValue);

        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Bentos.Add(new Bento { UserId = 10, Bento1 = 7, BentoDate = DateTime.UtcNow });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var found = await service.FindBentoAsync(10);

        Assert.True(found.HasValue);
        Assert.Equal(7, found.Value.Bento1);
        Assert.True(cache.TryGetValue(10L, out Bento? cached));
        Assert.Equal(7, cached!.Bento1);
    }

    [Fact]
    public async Task IncrementBentoAsync_CreatesAndUpdatesExisting()
    {
        var factory = new InfrastructureTestDbFactory();
        var service = CreateService(factory);

        await service.IncrementBentoAsync(10, 3);
        await service.IncrementBentoAsync(10, 4);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var bento = await db.Bentos.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(7, bento.Bento1);
    }

    [Fact]
    public async Task UpsertBentoAsync_CreatesAndThenIncrements()
    {
        var factory = new InfrastructureTestDbFactory();
        var service = CreateService(factory);

        var created = await service.UpsertBentoAsync(10, 3);
        var updated = await service.UpsertBentoAsync(10, 4);

        Assert.Equal(3, created.Bento1);
        Assert.Equal(7, updated.Bento1);
    }

    [Fact]
    public async Task UpdateBentoDateAsync_CreatesAndUpdatesDate()
    {
        var factory = new InfrastructureTestDbFactory();
        var service = CreateService(factory);
        var firstDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var secondDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);

        await service.UpdateBentoDateAsync(10, firstDate);
        await service.UpdateBentoDateAsync(10, secondDate);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var bento = await db.Bentos.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(secondDate, bento.BentoDate);
        Assert.Equal(0, bento.Bento1);
    }

    [Fact]
    public async Task CountsAndRank_ReturnExpectedValues()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Bentos.AddRange(
                new Bento { UserId = 1, Bento1 = 10, BentoDate = DateTime.UtcNow },
                new Bento { UserId = 2, Bento1 = 30, BentoDate = DateTime.UtcNow },
                new Bento { UserId = 3, Bento1 = 20, BentoDate = DateTime.UtcNow });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory);

        var count = await service.GetTotalCountOfBentoUsersAsync();
        var rank = await service.GetBentoRankAsync(3);
        var missingRank = await service.GetBentoRankAsync(999);

        Assert.Equal(3, count);
        Assert.True(rank.HasValue);
        Assert.Equal(2, rank.Value);
        Assert.True(missingRank.HasNoValue);
    }
}
