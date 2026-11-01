using System.Text;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Prometheus;

namespace dotBento.Infrastructure.Tests.Services;

public sealed class MemoryMetricsTests
{
    [Fact]
    public async Task Register_ExportsCurrentAggregateCountsWithFixedLabels()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set("user:123", "user");
        cache.Set("guild:456", "guild");
        var registry = Metrics.NewCustomRegistry();
        var members = 42;
        var guilds = 3;
        var channels = 11;
        MemoryMetrics.Register(cache, registry, () => members, () => 7, () => guilds, () => channels);

        async Task<string> Collect()
        {
            using var stream = new MemoryStream();
            await registry.CollectAndExportAsTextAsync(stream, TestContext.Current.CancellationToken);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        var first = await Collect();
        Assert.Contains("dotbento_memory_cache_entries{cache=\"application\"} 2", first);
        Assert.Contains("dotbento_memory_cache_entries{cache=\"spotify\"} 7", first);
        Assert.Contains("dotbento_discord_cached_guild_members 42", first);
        Assert.Contains("dotbento_discord_cached_guilds 3", first);
        Assert.Contains("dotbento_discord_cached_channels 11", first);
        Assert.DoesNotContain("user:123", first);
        Assert.DoesNotContain("guild:456", first);
        cache.Remove("user:123");
        members = 1;
        guilds = 1;
        channels = 4;
        var second = await Collect();
        Assert.Contains("dotbento_memory_cache_entries{cache=\"application\"} 1", second);
        Assert.Contains("dotbento_discord_cached_guild_members 1", second);
        Assert.Contains("dotbento_discord_cached_guilds 1", second);
        Assert.Contains("dotbento_discord_cached_channels 4", second);
    }
}
