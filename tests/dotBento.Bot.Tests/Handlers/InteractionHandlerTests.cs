using System.Runtime.CompilerServices;
using dotBento.Bot.Handlers;
using dotBento.Bot.Services;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using NetCord.Services.ApplicationCommands;

namespace dotBento.Bot.Tests.Handlers;

public sealed class InteractionHandlerTests
{
    private static InteractionHandler CreateHandler(
        GuildService guildService,
        UserService userService,
        GuildMemberLookupService memberLookup)
    {
        var handler = (InteractionHandler)RuntimeHelpers.GetUninitializedObject(typeof(InteractionHandler));
        HandlerFieldSetter.SetField(handler, "_userService", userService);
        HandlerFieldSetter.SetField(handler, "_guildService", guildService);
        HandlerFieldSetter.SetField(handler, "_memberLookup", memberLookup);
        HandlerFieldSetter.SetField(handler, "_client", NetCordFakes.CreateUninitializedGatewayClient());
        HandlerFieldSetter.SetField(handler, "_appCommands", null!);
        HandlerFieldSetter.SetField(handler, "_componentCommands", null!);
        HandlerFieldSetter.SetField(handler, "_modalCommands", null!);
        HandlerFieldSetter.SetField(handler, "_fergunInteractiveService", null!);
        HandlerFieldSetter.SetField(handler, "_provider", null!);
        return handler;
    }

    [Fact]
    public async Task WhenContextIsDm_NoDbWritesOccur()
    {
        const ulong userId = 601UL;
        var factory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(
            new GuildService(factory, cache),
            new UserService(cache, factory),
            new GuildMemberLookupService(NetCordFakes.CreateUninitializedGatewayClient(), cache));

        var interaction = NetCordFakes.CreateSlashCommandInteraction(userId, guildId: null);
        var context = new ApplicationCommandContext(interaction, NetCordFakes.CreateUninitializedGatewayClient());

        await handler.EnsureGuildAndUserExists(context);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Empty(db.Users.ToList());
        Assert.Empty(db.Guilds.ToList());
        Assert.Empty(db.GuildMembers.ToList());
    }

    [Fact]
    public async Task WhenUserIsABot_NoDbWritesOccur()
    {
        const ulong userId = 602UL;
        const ulong guildId = 702UL;
        var factory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(
            new GuildService(factory, cache),
            new UserService(cache, factory),
            new GuildMemberLookupService(NetCordFakes.CreateUninitializedGatewayClient(), cache));

        var interaction = NetCordFakes.CreateSlashCommandInteraction(userId, guildId, isBot: true);
        var context = new ApplicationCommandContext(interaction, NetCordFakes.CreateUninitializedGatewayClient());

        await handler.EnsureGuildAndUserExists(context);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Empty(db.Users.ToList());
        Assert.Empty(db.Guilds.ToList());
        Assert.Empty(db.GuildMembers.ToList());
    }

    [Fact]
    public async Task WhenContextHasGuildAndHumanUser_UserGuildAndMemberArePersisted()
    {
        const ulong userId = 603UL;
        const ulong guildId = 703UL;
        var factory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        // Pre-seed member cache so GetOrFetchAsync never falls through to REST
        cache.Set($"gm:{guildId}:{userId}", NetCordFakes.CreateGuildUser(userId, guildId));

        var handler = CreateHandler(
            new GuildService(factory, cache),
            new UserService(cache, factory),
            new GuildMemberLookupService(NetCordFakes.CreateUninitializedGatewayClient(), cache));

        var interaction = NetCordFakes.CreateSlashCommandInteraction(userId, guildId);
        var context = new ApplicationCommandContext(interaction, NetCordFakes.CreateUninitializedGatewayClient());

        await handler.EnsureGuildAndUserExists(context);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Single(db.Users.Where(u => u.UserId == (long)userId).ToList());
        Assert.Single(db.Guilds.Where(g => g.GuildId == (long)guildId).ToList());
        Assert.Single(db.GuildMembers.Where(m => m.UserId == (long)userId && m.GuildId == (long)guildId).ToList());
    }

    [Fact]
    public async Task WhenCalledTwiceForSameUser_NoDuplicateRowsCreated()
    {
        const ulong userId = 604UL;
        const ulong guildId = 704UL;
        var factory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set($"gm:{guildId}:{userId}", NetCordFakes.CreateGuildUser(userId, guildId));

        var handler = CreateHandler(
            new GuildService(factory, cache),
            new UserService(cache, factory),
            new GuildMemberLookupService(NetCordFakes.CreateUninitializedGatewayClient(), cache));

        var interaction = NetCordFakes.CreateSlashCommandInteraction(userId, guildId);
        var context = new ApplicationCommandContext(interaction, NetCordFakes.CreateUninitializedGatewayClient());

        await handler.EnsureGuildAndUserExists(context);
        await handler.EnsureGuildAndUserExists(context);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Single(db.Users.ToList());
        Assert.Single(db.Guilds.ToList());
        Assert.Single(db.GuildMembers.ToList());
    }

    [Fact]
    public async Task WhenContextHasGuild_CheckGuildOnlyReturnsTrue()
    {
        const ulong userId = 605UL;
        const ulong guildId = 705UL;
        var factory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(
            new GuildService(factory, cache),
            new UserService(cache, factory),
            new GuildMemberLookupService(NetCordFakes.CreateUninitializedGatewayClient(), cache));

        var interaction = NetCordFakes.CreateSlashCommandInteraction(userId, guildId);
        var context = new ApplicationCommandContext(interaction, NetCordFakes.CreateUninitializedGatewayClient());

        var result = await handler.CheckGuildOnly(context, interaction);

        Assert.True(result);
    }
}
