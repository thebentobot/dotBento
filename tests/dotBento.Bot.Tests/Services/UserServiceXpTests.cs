using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using EfGuild = dotBento.EntityFramework.Entities.Guild;
using EfUser = dotBento.EntityFramework.Entities.User;

namespace dotBento.Bot.Tests.Services;

public sealed class UserServiceXpTests
{
    private static async Task SeedUserAndMemberAsync(
        InMemoryDbFactory factory,
        long userId, long guildId,
        int xp = 0, int level = 1)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        db.Users.Add(new EfUser { UserId = userId, Discriminator = "User", Username = "tester", Xp = xp, Level = level });
        db.Guilds.Add(new EfGuild { GuildId = guildId, GuildName = "Test", Prefix = "?", MemberCount = 1 });
        db.GuildMembers.Add(new GuildMember { GuildId = guildId, UserId = userId, Xp = xp, Level = level });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task WhenUserSendsMessage_XpIsAddedToUserAndGuildMember()
    {
        const long userId = 401L;
        const long guildId = 501L;
        var factory = new InMemoryDbFactory();
        await SeedUserAndMemberAsync(factory, userId, guildId);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new UserService(cache, factory);

        await sut.AddExperienceAsync((ulong)userId, (ulong)guildId, Maybe<Patreon>.None);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var user = db.Users.Single(u => u.UserId == userId);
        var member = db.GuildMembers.Single(m => m.UserId == userId && m.GuildId == guildId);

        Assert.Equal(23, user.Xp);
        Assert.Equal(23, member.Xp);
    }

    [Fact]
    public async Task WhenUserSendsMessage_LevelIsUnchangedBeforeThreshold()
    {
        const long userId = 402L;
        const long guildId = 502L;
        var factory = new InMemoryDbFactory();
        await SeedUserAndMemberAsync(factory, userId, guildId, xp: 0, level: 1);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new UserService(cache, factory);

        await sut.AddExperienceAsync((ulong)userId, (ulong)guildId, Maybe<Patreon>.None);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var user = db.Users.Single(u => u.UserId == userId);
        Assert.Equal(1, user.Level);
    }

    [Fact]
    public async Task WhenXpReachesLevelThreshold_UserLevelsUp()
    {
        const long userId = 403L;
        const long guildId = 503L;
        // Level 1 needs 1*1*100 = 100 XP; seed at 77 so +23 = 100 → level up
        var factory = new InMemoryDbFactory();
        await SeedUserAndMemberAsync(factory, userId, guildId, xp: 77, level: 1);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new UserService(cache, factory);

        await sut.AddExperienceAsync((ulong)userId, (ulong)guildId, Maybe<Patreon>.None);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var user = db.Users.Single(u => u.UserId == userId);
        Assert.Equal(2, user.Level);
        Assert.Equal(0, user.Xp);
    }

    [Fact]
    public async Task WhenXpReachesLevelThreshold_GuildMemberLevelsUp()
    {
        const long userId = 404L;
        const long guildId = 504L;
        var factory = new InMemoryDbFactory();
        await SeedUserAndMemberAsync(factory, userId, guildId, xp: 77, level: 1);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new UserService(cache, factory);

        await sut.AddExperienceAsync((ulong)userId, (ulong)guildId, Maybe<Patreon>.None);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var member = db.GuildMembers.Single(m => m.UserId == userId && m.GuildId == guildId);
        Assert.Equal(2, member.Level);
        Assert.Equal(0, member.Xp);
    }

    [Fact]
    public async Task WhenUserOrGuildMemberDoesNotExist_NothingHappens()
    {
        var factory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new UserService(cache, factory);

        // No seeded data — should silently return
        await sut.AddExperienceAsync(9999UL, 8888UL, Maybe<Patreon>.None);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Empty(db.Users.ToList());
    }
}
