using dotBento.Bot.Handlers;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Bot.Tests.Handlers;

public sealed class MessageHandlerTests
{
    [Fact]
    public void TryBeginMessageXpCooldown_AllowsFirstMessageAndBlocksImmediateRepeat()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var first = MessageHandler.TryBeginMessageXpCooldown(cache, 123UL);
        var second = MessageHandler.TryBeginMessageXpCooldown(cache, 123UL);

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public void TryBeginMessageXpCooldown_IsPerUser()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var firstUser = MessageHandler.TryBeginMessageXpCooldown(cache, 123UL);
        var secondUser = MessageHandler.TryBeginMessageXpCooldown(cache, 456UL);

        Assert.True(firstUser);
        Assert.True(secondUser);
    }

    [Fact]
    public void TryBeginMessageTrackingCooldown_AllowsFirstGuildUserMessageAndBlocksImmediateRepeat()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var first = MessageHandler.TryBeginMessageTrackingCooldown(cache, guildId: 10UL, userId: 123UL);
        var second = MessageHandler.TryBeginMessageTrackingCooldown(cache, guildId: 10UL, userId: 123UL);

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public void TryBeginMessageTrackingCooldown_IsPerGuildUserPair()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var firstGuild = MessageHandler.TryBeginMessageTrackingCooldown(cache, guildId: 10UL, userId: 123UL);
        var secondGuild = MessageHandler.TryBeginMessageTrackingCooldown(cache, guildId: 20UL, userId: 123UL);
        var secondUser = MessageHandler.TryBeginMessageTrackingCooldown(cache, guildId: 10UL, userId: 456UL);

        Assert.True(firstGuild);
        Assert.True(secondGuild);
        Assert.True(secondUser);
    }
}
