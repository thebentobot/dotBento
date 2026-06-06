using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Discord;

namespace dotBento.Infrastructure.Tests.Services;

public class UserServiceTests
{
    private static UserService CreateService(
        InfrastructureTestDbFactory factory,
        IMemoryCache? cache = null) =>
        new(cache ?? new MemoryCache(new MemoryCacheOptions()), factory);

    private static User User(long id, int level = 1, int xp = 0) => new()
    {
        UserId = id,
        Username = $"User{id}",
        Discriminator = "0001",
        Level = level,
        Xp = xp
    };

    [Fact]
    public async Task GetUserMethods_ReturnExpectedMaybeValuesAndCache()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(User(10));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory, cache);

        var fromDatabase = await service.GetUserFromDatabaseAsync(10);
        var missing = await service.GetUserFromDatabaseAsync(999);
        var fromGetUser = await service.GetUserAsync(10);
        var fromCache = await service.GetUserFromCache(10);

        Assert.True(fromDatabase.HasValue);
        Assert.True(missing.HasNoValue);
        Assert.True(fromGetUser.HasValue);
        Assert.True(fromCache.HasValue);
        Assert.Equal(10, fromCache.Value.UserId);
    }

    [Fact]
    public async Task GetMultipleUsersAndAllUsers_ReturnDatabaseUsers()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.AddRange(User(1), User(2), User(3));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory);

        var multiple = await service.GetMultipleUsers([1, 3]);
        var all = await service.GetAllDiscordUserIds();
        var count = await service.GetTotalDatabaseUserCountAsync();

        Assert.Equal([1L, 3L], multiple.Keys.OrderBy(x => x));
        Assert.Equal(3, all.Count);
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetTotalDiscordUserCountAsync_SumsGuildMemberCounts()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Guilds.AddRange(
                new Guild { GuildId = 100, GuildName = "One", Prefix = "!", Leaderboard = true, Media = false, Tiktok = false, MemberCount = 10 },
                new Guild { GuildId = 200, GuildName = "Two", Prefix = "!", Leaderboard = true, Media = false, Tiktok = false, MemberCount = 15 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory);

        var count = await service.GetTotalDiscordUserCountAsync();

        Assert.True(count.HasValue);
        Assert.Equal(25, count.Value);
    }

    [Fact]
    public async Task DeleteUserAsync_RemovesExistingUserAndCacheEntry()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(User(10));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory, cache);
        _ = await service.GetUserAsync(10);

        await service.DeleteUserAsync(10);
        await service.DeleteUserAsync(999);

        await using var assertDb = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Empty(assertDb.Users);
        Assert.False(cache.TryGetValue("user-10", out _));
    }

    [Fact]
    public async Task GetPatreonUserAsync_ReturnsMaybe()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(User(10));
            db.Patreons.Add(new Patreon { UserId = 10, Name = "Patron", Avatar = "avatar.png", Follower = true });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory);

        var found = await service.GetPatreonUserAsync(10);
        var missing = await service.GetPatreonUserAsync(999);

        Assert.True(found.HasValue);
        Assert.True(missing.HasNoValue);
    }

    [Fact]
    public async Task GetUserRankAsync_ReturnsRankOrNone()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.AddRange(User(1, level: 3, xp: 0), User(2, level: 2, xp: 50), User(3, level: 2, xp: 10));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory);

        var rank = await service.GetUserRankAsync(3);
        var missing = await service.GetUserRankAsync(999);

        Assert.True(rank.HasValue);
        Assert.Equal(3, rank.Value);
        Assert.True(missing.HasNoValue);
    }

    [Fact]
    public async Task GetUserBatchAndUsersWithoutGuilds_ReturnExpectedUsers()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Guilds.Add(new Guild { GuildId = 100, GuildName = "Guild", Prefix = "!", Leaderboard = true, Media = false, Tiktok = false });
            db.Users.AddRange(User(1), User(2), User(3));
            db.GuildMembers.Add(new GuildMember { GuildId = 100, UserId = 2, Level = 1, Xp = 0 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory);

        var batch = await service.GetUserBatchAsync(batchSize: 2, skip: 1);
        var withoutGuilds = await service.GetUsersWithoutGuilds();

        Assert.Equal([2L, 3L], batch.Select(u => u.UserId));
        Assert.Equal([1L, 3L], withoutGuilds.OrderBy(x => x));
    }

    [Fact]
    public async Task GetNameAsync_UsesGlobalNameWhenGuildIsNull()
    {
        var user = new Mock<IUser>();
        user.SetupGet(x => x.GlobalName).Returns("Global");
        user.SetupGet(x => x.Username).Returns("Username");

        var name = await UserService.GetNameAsync(null, user.Object);

        Assert.Equal("Global", name);
    }

    [Fact]
    public async Task GetNameAsync_UsesGuildDisplayNameWhenGuildUserExists()
    {
        var user = new Mock<IUser>();
        user.SetupGet(x => x.Id).Returns(10);
        user.SetupGet(x => x.GlobalName).Returns("Global");
        user.SetupGet(x => x.Username).Returns("Username");
        var guildUser = new Mock<IGuildUser>();
        guildUser.SetupGet(x => x.DisplayName).Returns("Guild Name");
        var guild = new Mock<IGuild>();
        guild.Setup(x => x.GetUserAsync(10, CacheMode.AllowDownload, null))
            .ReturnsAsync(guildUser.Object);

        var name = await UserService.GetNameAsync(guild.Object, user.Object);

        Assert.Equal("Guild Name", name);
    }

    [Fact]
    public async Task GetNameAsync_FallsBackWhenGuildUserIsMissing()
    {
        var user = new Mock<IUser>();
        user.SetupGet(x => x.Id).Returns(10);
        string nullGlobalName = null!;
        user.SetupGet(x => x.GlobalName).Returns(nullGlobalName);
        user.SetupGet(x => x.Username).Returns("Username");
        var guild = new Mock<IGuild>();
        guild.Setup(x => x.GetUserAsync(10, CacheMode.AllowDownload, null))
            .ReturnsAsync((IGuildUser?)null);

        var name = await UserService.GetNameAsync(guild.Object, user.Object);

        Assert.Equal("Username", name);
    }

    [Fact]
    public async Task SyncUserFromDiscordAsync_UpdatesChangedFieldsAndRemovesCacheEntry()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(new User
            {
                UserId = 10,
                Username = "Old",
                Discriminator = "0001",
                AvatarUrl = "old.png",
                Level = 1,
                Xp = 0
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory, cache);
        _ = await service.GetUserAsync(10);
        var discordUser = new Mock<IUser>();
        discordUser.SetupGet(x => x.Username).Returns("New");
        discordUser.SetupGet(x => x.Discriminator).Returns("1234");
        discordUser.Setup(x => x.GetAvatarUrl(ImageFormat.Auto, 512)).Returns("new.png");

        var changed = await service.SyncUserFromDiscordAsync(new User { UserId = 10 }, discordUser.Object);

        Assert.True(changed);
        Assert.False(cache.TryGetValue("user-10", out _));
        await using var assertDb = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var user = await assertDb.Users.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(("New", "1234", "new.png"), (user.Username, user.Discriminator, user.AvatarUrl));
    }

    [Fact]
    public async Task SyncUserFromDiscordAsync_ReturnsFalseForNoChangesOrMissingUser()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(new User
            {
                UserId = 10,
                Username = "Same",
                Discriminator = "0001",
                AvatarUrl = "same.png",
                Level = 1,
                Xp = 0
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory);
        var discordUser = new Mock<IUser>();
        discordUser.SetupGet(x => x.Username).Returns("Same");
        discordUser.SetupGet(x => x.Discriminator).Returns("0001");
        discordUser.Setup(x => x.GetAvatarUrl(ImageFormat.Auto, 512)).Returns("same.png");

        var unchanged = await service.SyncUserFromDiscordAsync(new User { UserId = 10 }, discordUser.Object);
        var missing = await service.SyncUserFromDiscordAsync(new User { UserId = 999 }, discordUser.Object);

        Assert.False(unchanged);
        Assert.False(missing);
    }
}
