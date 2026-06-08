using System.Runtime.CompilerServices;
using dotBento.Bot.Handlers;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using EfUser = dotBento.EntityFramework.Entities.User;

namespace dotBento.Bot.Tests.Handlers;

public sealed class UserUpdateHandlerTests
{
    private static UserUpdateHandler CreateHandler(UserService userService)
    {
        var handler = (UserUpdateHandler)RuntimeHelpers.GetUninitializedObject(typeof(UserUpdateHandler));
        HandlerFieldSetter.SetField(handler, "_userService", userService);
        HandlerFieldSetter.SetField(handler, "_client", NetCordFakes.CreateUninitializedGatewayClient());
        return handler;
    }

    [Fact]
    public async Task WhenUserAvatarChanges_AvatarUrlIsUpdatedInDb()
    {
        const long userId = 301L;
        const string newAvatarHash = "newavatar";
        var expectedUrl = $"https://cdn.discordapp.com/avatars/{userId}/{newAvatarHash}.webp?size=512";

        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(new EfUser
            {
                UserId = userId, Discriminator = "User", Username = "alice",
                Level = 1, Xp = 0, AvatarUrl = "https://example.com/old.png"
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(new UserService(cache, factory));
        var user = NetCordFakes.CreateGuildUser((ulong)userId, 0UL, username: "alice", avatarHash: newAvatarHash);

        await handler.UserUpdated(user);

        await using var db2 = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal(expectedUrl, db2.Users.Single().AvatarUrl);
    }

    [Fact]
    public async Task WhenAvatarIsUnchanged_AvatarUrlIsNotUpdated()
    {
        const long userId = 302L;
        const string avatarHash = "samehash";
        var avatarUrl = $"https://cdn.discordapp.com/avatars/{userId}/{avatarHash}.webp?size=512";

        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(new EfUser
            {
                UserId = userId, Discriminator = "User", Username = "bob",
                Level = 1, Xp = 0, AvatarUrl = avatarUrl
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(new UserService(cache, factory));
        var user = NetCordFakes.CreateGuildUser((ulong)userId, 0UL, username: "bob", avatarHash: avatarHash);

        await handler.UserUpdated(user);

        await using var db2 = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal(avatarUrl, db2.Users.Single().AvatarUrl);
    }

    [Fact]
    public async Task WhenUsernameChanges_UsernameIsUpdatedInDb()
    {
        const long userId = 303L;

        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(new EfUser
            {
                UserId = userId, Discriminator = "User", Username = "oldname",
                Level = 1, Xp = 0
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(new UserService(cache, factory));
        var user = NetCordFakes.CreateGuildUser((ulong)userId, 0UL, username: "newname");

        await handler.UserUpdated(user);

        await using var db2 = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal("newname", db2.Users.Single().Username);
    }

    [Fact]
    public async Task WhenUserIsABot_NoDbUpdateOccurs()
    {
        const long userId = 304L;

        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(new EfUser
            {
                UserId = userId, Discriminator = "Bot", Username = "somebot",
                Level = 1, Xp = 0, AvatarUrl = "https://example.com/bot.png"
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(new UserService(cache, factory));
        var botUser = NetCordFakes.CreateGuildUser((ulong)userId, 0UL,
            isBot: true, username: "somebot", avatarHash: "newhash");

        await handler.UserUpdated(botUser);

        await using var db2 = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal("https://example.com/bot.png", db2.Users.Single().AvatarUrl);
    }

    [Fact]
    public async Task WhenUserIsNotInDb_NoExceptionIsThrown()
    {
        var factory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(new UserService(cache, factory));
        var user = NetCordFakes.CreateGuildUser(9999UL, 0UL, username: "ghost", avatarHash: "abc");

        await handler.UserUpdated(user);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Empty(db.Users.ToList());
    }
}
