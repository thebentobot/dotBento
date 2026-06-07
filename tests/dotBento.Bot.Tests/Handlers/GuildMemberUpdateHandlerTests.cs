using System.Runtime.CompilerServices;
using dotBento.Bot.Handlers;
using dotBento.Bot.Services;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Bot.Tests.Handlers;

public sealed class GuildMemberUpdateHandlerTests
{
    private static GuildMemberUpdateHandler CreateHandler(
        GuildService guildService, IMemoryCache cache)
    {
        var handler = (GuildMemberUpdateHandler)RuntimeHelpers.GetUninitializedObject(
            typeof(GuildMemberUpdateHandler));
        var memberLookup = new GuildMemberLookupService(null!, cache);
        HandlerFieldSetter.SetField(handler, "_guildService", guildService);
        HandlerFieldSetter.SetField(handler, "_memberLookup", memberLookup);
        HandlerFieldSetter.SetField(handler, "_client", NetCordFakes.CreateUninitializedGatewayClient());
        return handler;
    }

    [Fact]
    public async Task WhenGuildMemberAvatarChanges_AvatarUrlIsUpdatedInDb()
    {
        const long userId = 101L;
        const long guildId = 201L;
        const string oldAvatarUrl = "https://cdn.discordapp.com/guilds/201/users/101/avatars/oldhash.webp?size=512";
        const string newGuildAvatarHash = "newhash";
        var expectedNewUrl = $"https://cdn.discordapp.com/guilds/{guildId}/users/{userId}/avatars/{newGuildAvatarHash}.webp?size=512";

        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.GuildMembers.Add(new GuildMember
            {
                GuildId = guildId, UserId = userId, Level = 1, Xp = 0, AvatarUrl = oldAvatarUrl
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(new GuildService(factory, cache), cache);
        var member = NetCordFakes.CreateGuildUser((ulong)userId, (ulong)guildId,
            guildAvatarHash: newGuildAvatarHash);

        await handler.GuildUserUpdated(member);

        await using var db2 = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var saved = db2.GuildMembers.Single();
        Assert.Equal(expectedNewUrl, saved.AvatarUrl);
    }

    [Fact]
    public async Task WhenGuildMemberAvatarIsUnchanged_AvatarUrlIsNotUpdated()
    {
        const long userId = 102L;
        const long guildId = 202L;
        const string guildAvatarHash = "samehash";
        var avatarUrl = $"https://cdn.discordapp.com/guilds/{guildId}/users/{userId}/avatars/{guildAvatarHash}.webp?size=512";

        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.GuildMembers.Add(new GuildMember
            {
                GuildId = guildId, UserId = userId, Level = 1, Xp = 0, AvatarUrl = avatarUrl
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(new GuildService(factory, cache), cache);
        var member = NetCordFakes.CreateGuildUser((ulong)userId, (ulong)guildId,
            guildAvatarHash: guildAvatarHash);

        await handler.GuildUserUpdated(member);

        await using var db2 = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal(avatarUrl, db2.GuildMembers.Single().AvatarUrl);
    }

    [Fact]
    public async Task WhenMemberIsABot_NoDbUpdateOccurs()
    {
        const long userId = 103L;
        const long guildId = 203L;
        const string originalUrl = "https://example.com/old.png";

        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.GuildMembers.Add(new GuildMember
            {
                GuildId = guildId, UserId = userId, Level = 1, Xp = 0, AvatarUrl = originalUrl
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(new GuildService(factory, cache), cache);
        var botMember = NetCordFakes.CreateGuildUser((ulong)userId, (ulong)guildId,
            isBot: true, guildAvatarHash: "newhash");

        await handler.GuildUserUpdated(botMember);

        await using var db2 = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal(originalUrl, db2.GuildMembers.Single().AvatarUrl);
    }

    [Fact]
    public async Task WhenMemberIsNotInDb_NoExceptionIsThrown()
    {
        var factory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(new GuildService(factory, cache), cache);
        var member = NetCordFakes.CreateGuildUser(999UL, 888UL, guildAvatarHash: "hash");

        await handler.GuildUserUpdated(member);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Empty(db.GuildMembers.ToList());
    }
}
