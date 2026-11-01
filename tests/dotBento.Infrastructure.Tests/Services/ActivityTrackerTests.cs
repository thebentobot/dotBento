using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace dotBento.Infrastructure.Tests.Services;

public sealed class ActivityTrackerTests
{
    private static IDistributedCache CreateCache() =>
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    [Fact]
    public async Task GetActiveUserCountAsync_CountsDistinctUsersPerWindow()
    {
        var tracker = new ActivityTracker(CreateCache());

        tracker.Record(1);
        tracker.Record(2);
        tracker.Record(1);

        Assert.Equal(2, await tracker.GetActiveUserCountAsync(1));
        Assert.Equal(2, await tracker.GetActiveUserCountAsync(7));
    }

    [Fact]
    public async Task FlushAsync_PersistsDaysSoASecondProcessSeesThem()
    {
        var cache = CreateCache();
        var first = new ActivityTracker(cache);
        first.Record(10);
        first.Record(20);
        await first.FlushAsync();

        var second = new ActivityTracker(cache);

        Assert.Equal(2, await second.GetActiveUserCountAsync(1));
        Assert.Equal(2, await second.GetActiveUserCountAsync(30));
    }

    [Fact]
    public async Task GetActiveUserCountAsync_IgnoresDaysOlderThanTheWindow()
    {
        var cache = CreateCache();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var oldDay = today.AddDays(-10);
        await cache.SetAsync($"activity:users:{oldDay:yyyy-MM-dd}", "[42]"u8.ToArray());
        await cache.SetAsync($"activity:users:{today:yyyy-MM-dd}", "[7]"u8.ToArray());

        var tracker = new ActivityTracker(cache);

        Assert.Equal(1, await tracker.GetActiveUserCountAsync(1));
        Assert.Equal(1, await tracker.GetActiveUserCountAsync(7));
        Assert.Equal(2, await tracker.GetActiveUserCountAsync(30));
    }

    [Fact]
    public async Task FlushAsync_SurvivesCorruptCacheEntry()
    {
        var cache = CreateCache();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await cache.SetAsync($"activity:users:{today:yyyy-MM-dd}", "not json"u8.ToArray());

        var tracker = new ActivityTracker(cache);
        tracker.Record(5);

        Assert.Equal(1, await tracker.GetActiveUserCountAsync(1));
        await tracker.FlushAsync();
    }
}
