using System.Runtime.CompilerServices;
using dotBento.Bot.Handlers;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Bot.Tests.Handlers;

public sealed class ClientLeftGuildHandlerTests
{
    private static ClientLeftGuildHandler CreateHandler(
        GuildService guildService, IMemoryCache cache)
    {
        var handler = (ClientLeftGuildHandler)RuntimeHelpers.GetUninitializedObject(
            typeof(ClientLeftGuildHandler));
        HandlerFieldSetter.SetField(handler, "_guildService", guildService);
        HandlerFieldSetter.SetField(handler, "_cache", cache);
        HandlerFieldSetter.SetField(handler, "_client", NetCordFakes.CreateUninitializedGatewayClient());
        return handler;
    }

    [Fact]
    public async Task WhenBotLeavesGuild_GuildIsDeletedFromDb()
    {
        const long guildId = 555L;
        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Guilds.Add(new Guild { GuildId = guildId, GuildName = "ToDelete", Prefix = "?", MemberCount = 5 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(new GuildService(factory, cache), cache);

        await handler.ClientLeftGuild((ulong)guildId);

        await using var db2 = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Empty(db2.Guilds.ToList());
    }

    [Fact]
    public async Task WhenKeepDataCacheKeyIsSet_GuildIsNotDeleted()
    {
        const long guildId = 666L;
        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Guilds.Add(new Guild { GuildId = guildId, GuildName = "KeepThis", Prefix = "?", MemberCount = 3 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set($"{guildId}-keep-data", true);

        var handler = CreateHandler(new GuildService(factory, cache), cache);

        await handler.ClientLeftGuild((ulong)guildId);

        await using var db2 = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Single(db2.Guilds.ToList());
    }
}
