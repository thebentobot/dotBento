using Microsoft.Extensions.Caching.Memory;
using Prometheus;

namespace dotBento.Infrastructure.Services;

public static class MemoryMetrics
{
    public static void Register(IMemoryCache cache, CollectorRegistry registry,
        Func<int>? cachedGuildMembers = null, Func<int>? spotifyEntries = null)
    {
        var factory = Metrics.WithCustomRegistry(registry);
        var entries = factory.CreateGauge("dotbento_memory_cache_entries",
            "Number of entries in a local application cache", new GaugeConfiguration { LabelNames = ["cache"] });
        var members = cachedGuildMembers is null ? null : factory.CreateGauge(
            "dotbento_discord_cached_guild_members", "Total cached guild members across all Discord guilds");

        registry.AddBeforeCollectCallback(() =>
        {
            if (cache is MemoryCache memoryCache)
                entries.WithLabels("application").Set(memoryCache.Count);
            if (spotifyEntries is not null)
                entries.WithLabels("spotify").Set(spotifyEntries());
            if (cachedGuildMembers is not null)
                members!.Set(cachedGuildMembers());
        });
    }
}
