using System.Runtime.CompilerServices;
using dotBento.Bot.Handlers;
using dotBento.Bot.Services;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using EfGuild = dotBento.EntityFramework.Entities.Guild;
using EfUser = dotBento.EntityFramework.Entities.User;

namespace dotBento.Bot.Tests.Handlers;

public sealed class GuildMemberRemoveHandlerTests
{
    private static GuildMemberRemoveHandler CreateHandler(
        GuildService guildService, UserService userService, IMemoryCache cache)
    {
        var handler = (GuildMemberRemoveHandler)RuntimeHelpers.GetUninitializedObject(
            typeof(GuildMemberRemoveHandler));
        var memberLookup = new GuildMemberLookupService(null!, cache);
        HandlerFieldSetter.SetField(handler, "_guildService", guildService);
        HandlerFieldSetter.SetField(handler, "_userService", userService);
        HandlerFieldSetter.SetField(handler, "_memberLookup", memberLookup);
        return handler;
    }

    private static async Task SeedSingleGuildMemberAsync(
        InMemoryDbFactory factory, long userId, long guildId)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        db.Users.Add(new EfUser { UserId = userId, Discriminator = "User", Username = "tester", Level = 1, Xp = 0 });
        db.Guilds.Add(new EfGuild { GuildId = guildId, GuildName = "Test Guild", Prefix = "?", MemberCount = 1 });
        db.GuildMembers.Add(new GuildMember { GuildId = guildId, UserId = userId, Level = 1, Xp = 0 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task WhenMemberLeaves_GuildMemberIsDeletedFromDb()
    {
        var factory = new InMemoryDbFactory();
        const long userId = 111L;
        const long guildId = 222L;
        await SeedSingleGuildMemberAsync(factory, userId, guildId);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(
            new GuildService(factory, cache),
            new UserService(cache, factory),
            cache);

        await handler.GuildMemberRemoved((ulong)guildId, (ulong)userId);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Empty(db.GuildMembers.ToList());
    }

    [Fact]
    public async Task WhenMemberLeavesAndUserIsInNoOtherGuilds_UserIsDeletedFromDb()
    {
        var factory = new InMemoryDbFactory();
        const long userId = 111L;
        const long guildId = 222L;
        await SeedSingleGuildMemberAsync(factory, userId, guildId);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(
            new GuildService(factory, cache),
            new UserService(cache, factory),
            cache);

        await handler.GuildMemberRemoved((ulong)guildId, (ulong)userId);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Empty(db.Users.ToList());
    }

    [Fact]
    public async Task WhenMemberLeavesAndUserIsInAnotherGuild_UserIsRetained()
    {
        var factory = new InMemoryDbFactory();
        const long userId = 111L;
        const long guildId = 222L;
        const long otherGuildId = 333L;

        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(new EfUser { UserId = userId, Discriminator = "User", Username = "tester", Level = 1, Xp = 0 });
            db.Guilds.Add(new EfGuild { GuildId = guildId, GuildName = "Guild A", Prefix = "?", MemberCount = 1 });
            db.Guilds.Add(new EfGuild { GuildId = otherGuildId, GuildName = "Guild B", Prefix = "?", MemberCount = 1 });
            db.GuildMembers.Add(new GuildMember { GuildId = guildId, UserId = userId, Level = 1, Xp = 0 });
            db.GuildMembers.Add(new GuildMember { GuildId = otherGuildId, UserId = userId, Level = 1, Xp = 0 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(
            new GuildService(factory, cache),
            new UserService(cache, factory),
            cache);

        await handler.GuildMemberRemoved((ulong)guildId, (ulong)userId);

        await using var db2 = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var user = db2.Users.SingleOrDefault(u => u.UserId == userId);
        Assert.NotNull(user);
    }

    [Fact]
    public async Task WhenMemberLeavesAndUserIsInAnotherGuild_OnlyThatGuildsRecordIsRemoved()
    {
        var factory = new InMemoryDbFactory();
        const long userId = 111L;
        const long guildId = 222L;
        const long otherGuildId = 333L;

        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(new EfUser { UserId = userId, Discriminator = "User", Username = "tester", Level = 1, Xp = 0 });
            db.Guilds.Add(new EfGuild { GuildId = guildId, GuildName = "Guild A", Prefix = "?", MemberCount = 1 });
            db.Guilds.Add(new EfGuild { GuildId = otherGuildId, GuildName = "Guild B", Prefix = "?", MemberCount = 1 });
            db.GuildMembers.Add(new GuildMember { GuildId = guildId, UserId = userId, Level = 1, Xp = 0 });
            db.GuildMembers.Add(new GuildMember { GuildId = otherGuildId, UserId = userId, Level = 1, Xp = 0 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(
            new GuildService(factory, cache),
            new UserService(cache, factory),
            cache);

        await handler.GuildMemberRemoved((ulong)guildId, (ulong)userId);

        await using var db2 = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var remainingMembers = db2.GuildMembers.ToList();
        Assert.Single(remainingMembers);
        Assert.Equal(otherGuildId, remainingMembers[0].GuildId);
    }
}
