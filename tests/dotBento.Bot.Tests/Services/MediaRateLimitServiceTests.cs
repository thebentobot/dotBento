using dotBento.Bot.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Tests.Services;

public sealed class MediaRateLimitServiceTests
{
    private static (MediaRateLimitService Service, IDistributedCache Cache) CreateService()
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return (new MediaRateLimitService(cache), cache);
    }

    [Fact]
    public async Task CheckAndRecordAsync_AllowsFirstRequest()
    {
        var (service, _) = CreateService();

        var result = await service.CheckAndRecordAsync(10, 100, "twitter", guildMemberCount: 10);

        Assert.True(result.IsAllowed);
        Assert.Null(result.LimitType);
    }

    [Fact]
    public async Task CheckAndRecordAsync_DeniesImmediateSecondUserRequest()
    {
        var (service, _) = CreateService();
        await service.CheckAndRecordAsync(10, 100, "twitter", guildMemberCount: 10);

        var result = await service.CheckAndRecordAsync(10, 100, "twitter", guildMemberCount: 10);

        Assert.False(result.IsAllowed);
        Assert.Equal("user", result.LimitType);
        Assert.True(result.RetryAfter > TimeSpan.Zero);
    }

    [Fact]
    public async Task CheckAndRecordAsync_RateLimitsGuildWindow()
    {
        var (service, _) = CreateService();

        for (ulong userId = 1; userId <= 5; userId++)
        {
            var allowed = await service.CheckAndRecordAsync(userId, 100, "twitter", guildMemberCount: 10);
            Assert.True(allowed.IsAllowed);
        }

        var denied = await service.CheckAndRecordAsync(6, 100, "twitter", guildMemberCount: 10);

        Assert.False(denied.IsAllowed);
        Assert.Equal("guild", denied.LimitType);
    }

    [Theory]
    [InlineData(3, 120)]
    [InlineData(6, 300)]
    [InlineData(9, 600)]
    [InlineData(12, 1800)]
    public async Task CheckAndRecordAsync_UsesEscalatingCooldownTiers(int violations, int expectedCooldownSeconds)
    {
        var (service, cache) = CreateService();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await cache.SetStringAsync("media:rl:user:10:twitter", now.ToString(), TestContext.Current.CancellationToken);
        await cache.SetStringAsync("media:rl:violations:10:twitter", violations.ToString(), TestContext.Current.CancellationToken);

        var result = await service.CheckAndRecordAsync(10, guildId: null, "twitter");

        Assert.False(result.IsAllowed);
        Assert.Equal("user", result.LimitType);
        Assert.True(result.RetryAfter!.Value.TotalSeconds <= expectedCooldownSeconds);
        Assert.True(result.RetryAfter!.Value.TotalSeconds > expectedCooldownSeconds - 5);
    }

    [Theory]
    [InlineData(1_000, 7)]
    [InlineData(10_000, 10)]
    [InlineData(20_000, 15)]
    [InlineData(30_000, 20)]
    public async Task CheckAndRecordAsync_AppliesGuildLimitTiers(int memberCount, int expectedLimit)
    {
        var (service, _) = CreateService();

        for (ulong userId = 1; userId <= (ulong)expectedLimit; userId++)
        {
            var allowed = await service.CheckAndRecordAsync(userId, 100, "twitter", memberCount);
            Assert.True(allowed.IsAllowed);
        }

        var denied = await service.CheckAndRecordAsync(10_000, 100, "twitter", memberCount);

        Assert.False(denied.IsAllowed);
        Assert.Equal("guild", denied.LimitType);
    }
}
