using System.Runtime.CompilerServices;
using dotBento.Bot.Handlers;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Bot.Tests.Handlers;

public sealed class ClientJoinedGuildHandlerTests
{
    private static ClientJoinedGuildHandler CreateHandler(GuildService guildService)
    {
        var handler = (ClientJoinedGuildHandler)RuntimeHelpers.GetUninitializedObject(
            typeof(ClientJoinedGuildHandler));
        HandlerFieldSetter.SetField(handler, "_guildService", guildService);
        HandlerFieldSetter.SetField(handler, "_client", NetCordFakes.CreateUninitializedGatewayClient());
        return handler;
    }

    [Fact]
    public async Task WhenBotJoinsNewGuild_GuildIsAddedToDb()
    {
        const ulong guildId = 777777UL;
        var factory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(new GuildService(factory, cache));

        var guild = NetCordFakes.CreateGatewayGuild(guildId, "New Guild", userCount: 10);

        await handler.ClientJoinedGuild(guild);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var saved = db.Guilds.SingleOrDefault(g => g.GuildId == (long)guildId);
        Assert.NotNull(saved);
        Assert.Equal("New Guild", saved.GuildName);
        Assert.Equal(10, saved.MemberCount);
    }

    [Fact]
    public async Task WhenBotJoinsAlreadyExistingGuild_GuildIsNotDuplicated()
    {
        const ulong guildId = 888888UL;
        var factory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());

        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Guilds.Add(new dotBento.EntityFramework.Entities.Guild
            {
                GuildId = (long)guildId, GuildName = "Existing", Prefix = "?", MemberCount = 5
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var handler = CreateHandler(new GuildService(factory, cache));
        var guild = NetCordFakes.CreateGatewayGuild(guildId, "Existing", userCount: 5);

        await handler.ClientJoinedGuild(guild);

        await using var db2 = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Single(db2.Guilds.ToList());
    }
}
