using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace dotBento.Infrastructure.Services;

/// <summary>
/// Remembers which users interacted with the bot on which UTC day so dashboards can report
/// daily, weekly and monthly active users.
///
/// The day sets are buffered in memory and persisted to the distributed cache, which keeps the
/// counts across restarts without writing to PostgreSQL on every interaction. A process that
/// restarts mid-day loses at most the interactions since the last flush.
/// </summary>
public sealed class ActivityTracker(IDistributedCache cache)
{
    private const int RetentionDays = 40;

    private readonly ConcurrentDictionary<DateOnly, ConcurrentDictionary<ulong, byte>> _days = new();
    private readonly ConcurrentDictionary<DateOnly, byte> _dirty = new();
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private volatile bool _loaded;

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Records that a user interacted with the bot today.</summary>
    public void Record(ulong userId)
    {
        var today = Today;
        var users = _days.GetOrAdd(today, _ => new ConcurrentDictionary<ulong, byte>());
        if (users.TryAdd(userId, 0))
            _dirty[today] = 0;
    }

    /// <summary>Distinct users that interacted on any of the last <paramref name="days"/> UTC days.</summary>
    public async Task<int> GetActiveUserCountAsync(int days, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        var since = Today.AddDays(-(days - 1));
        var users = new HashSet<ulong>();
        foreach (var (day, set) in _days)
        {
            if (day < since)
                continue;

            foreach (var userId in set.Keys)
                users.Add(userId);
        }

        return users.Count;
    }

    /// <summary>Writes the day sets that changed since the last flush to the distributed cache.</summary>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        foreach (var day in _dirty.Keys.ToArray())
        {
            if (!_days.TryGetValue(day, out var users))
            {
                _dirty.TryRemove(day, out _);
                continue;
            }

            var payload = JsonSerializer.SerializeToUtf8Bytes(users.Keys.ToArray());
            await cache.SetAsync(Key(day), payload, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(RetentionDays)
            }, cancellationToken);

            _dirty.TryRemove(day, out _);
        }

        var cutoff = Today.AddDays(-RetentionDays);
        foreach (var day in _days.Keys)
        {
            if (day < cutoff)
                _days.TryRemove(day, out _);
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded)
            return;

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (_loaded)
                return;

            var today = Today;
            for (var offset = 0; offset < RetentionDays; offset++)
            {
                var day = today.AddDays(-offset);
                var payload = await cache.GetAsync(Key(day), cancellationToken);
                if (payload is null || payload.Length == 0)
                    continue;

                ulong[] userIds;
                try
                {
                    userIds = JsonSerializer.Deserialize<ulong[]>(payload) ?? [];
                }
                catch (JsonException)
                {
                    // A corrupt entry should not stop the bot from tracking activity.
                    continue;
                }

                var users = _days.GetOrAdd(day, _ => new ConcurrentDictionary<ulong, byte>());
                foreach (var userId in userIds)
                    users.TryAdd(userId, 0);
            }

            _loaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private static string Key(DateOnly day) => $"activity:users:{day:yyyy-MM-dd}";
}
