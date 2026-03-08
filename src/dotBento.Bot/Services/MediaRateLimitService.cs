using Microsoft.Extensions.Caching.Distributed;
using Prometheus;
using Serilog;

namespace dotBento.Bot.Services;

public sealed record RateLimitResult(bool IsAllowed, TimeSpan? RetryAfter = null, string? LimitType = null)
{
    public static RateLimitResult Allowed() => new(true);
    public static RateLimitResult DeniedUser(TimeSpan retryAfter) => new(false, retryAfter, "user");
    public static RateLimitResult DeniedGuild() => new(false, null, "guild");
}

/// <summary>
/// Rate limits media commands per-platform per-user: a TikTok use does not block a Twitter use.
/// Cooldowns escalate when a user repeatedly hits the limit:
///   0–2 violations → 60 s, 3–5 → 120 s, 6–8 → 300 s, 9–11 → 600 s, 12+ → 1800 s.
/// Violation counts are stored in Valkey with a 24-hour TTL.
/// Also enforces a per-guild fixed-window limit of 5 requests per minute across all platforms.
/// </summary>
public sealed class MediaRateLimitService(IDistributedCache cache)
{
    private static readonly Counter UserRateLimitHits = Metrics.CreateCounter(
        "dotbento_media_user_ratelimit_hits_total",
        "Number of times the per-user media command rate limit was triggered",
        new CounterConfiguration { LabelNames = ["platform"] });

    private static readonly Counter GuildRateLimitHits = Metrics.CreateCounter(
        "dotbento_media_guild_ratelimit_hits_total",
        "Number of times the per-guild media command rate limit was triggered",
        new CounterConfiguration { LabelNames = ["platform"] });

    private static int CooldownSeconds(int violations) => violations switch
    {
        < 3  => 60,    // 1 min
        < 6  => 120,   // 2 min
        < 9  => 300,   // 5 min
        < 12 => 600,   // 10 min
        _    => 1800,  // 30 min
    };

    private static int GuildLimit(int memberCount) => memberCount switch
    {
        >= 30_000 => 20,
        >= 20_000 => 15,
        >= 10_000 => 10,
        >= 1_000  => 7,
        _         => 5,
    };

    public async Task<RateLimitResult> CheckAndRecordAsync(ulong userId, ulong? guildId, string platform, int guildMemberCount = 0)
    {
        var now = DateTimeOffset.UtcNow;

        // --- per-user, per-platform cooldown ---
        var userKey       = $"media:rl:user:{userId}:{platform}";
        var violationsKey = $"media:rl:violations:{userId}:{platform}";

        var lastRequestStr = await cache.GetStringAsync(userKey);
        if (lastRequestStr is not null && long.TryParse(lastRequestStr, out var lastEpoch))
        {
            var lastRequest = DateTimeOffset.FromUnixTimeSeconds(lastEpoch);
            var elapsed     = now - lastRequest;

            var violationsStr = await cache.GetStringAsync(violationsKey);
            var violations    = violationsStr is not null && int.TryParse(violationsStr, out var v) ? v : 0;
            var cooldown      = CooldownSeconds(violations);

            if (elapsed.TotalSeconds < cooldown)
            {
                var retryAfter   = TimeSpan.FromSeconds(cooldown) - elapsed;
                var newViolations = violations + 1;

                // Reset the 24-hour violation window on each new violation
                await cache.SetStringAsync(violationsKey, newViolations.ToString(),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
                    });

                UserRateLimitHits.WithLabels(platform).Inc();
                Log.Information(
                    "Media user rate limit hit: userId={UserId} platform={Platform} violations={Violations} cooldownSeconds={Cooldown} retryAfterSeconds={RetryAfter}",
                    userId, platform, newViolations, cooldown, (int)retryAfter.TotalSeconds);
                return RateLimitResult.DeniedUser(retryAfter);
            }
        }

        // --- per-guild fixed-window limit (all platforms combined) ---
        if (guildId.HasValue)
        {
            var windowMinute  = now.ToUnixTimeSeconds() / 60;
            var guildCountKey = $"media:rl:guild:{guildId}:{windowMinute}";

            var countStr = await cache.GetStringAsync(guildCountKey);
            var count    = countStr is not null && int.TryParse(countStr, out var c) ? c : 0;
            var limit    = GuildLimit(guildMemberCount);

            if (count >= limit)
            {
                GuildRateLimitHits.WithLabels(platform).Inc();
                Log.Information(
                    "Media guild rate limit hit: guildId={GuildId} platform={Platform} requestsThisMinute={Count}",
                    guildId, platform, count);
                return RateLimitResult.DeniedGuild();
            }

            // Entries expire after 2 minutes — one minute to cover the window, one for cleanup
            await cache.SetStringAsync(guildCountKey, (count + 1).ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
                });
        }

        // Record this request; TTL matches the maximum possible cooldown so it never expires early
        await cache.SetStringAsync(userKey, now.ToUnixTimeSeconds().ToString(),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CooldownSeconds(99)),
            });

        return RateLimitResult.Allowed();
    }
}
