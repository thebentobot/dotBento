using dotBento.Domain;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace dotBento.Infrastructure.Tests.Services;

public class PrefixServiceTests
{
    [Fact]
    public void StoreGetAndRemovePrefix_UpdatesDictionary()
    {
        var factory = new InfrastructureTestDbFactory();
        var service = new PrefixService(factory);
        var guildId = (ulong)Random.Shared.NextInt64(100_000, 999_999);

        Assert.Equal(Constants.StartPrefix, service.GetPrefix(guildId));

        service.StorePrefix("?", guildId);
        Assert.Equal("?", service.GetPrefix(guildId));

        service.StorePrefix("$", guildId);
        Assert.Equal("$", service.GetPrefix(guildId));

        service.RemovePrefix(guildId);
        Assert.Equal(Constants.StartPrefix, service.GetPrefix(guildId));

        service.RemovePrefix(guildId);
        Assert.Equal(Constants.StartPrefix, service.GetPrefix(guildId));
    }

    [Fact]
    public void GetPrefix_WithNullGuild_ReturnsDefaultPrefix()
    {
        var service = new PrefixService(new InfrastructureTestDbFactory());

        Assert.Equal(Constants.StartPrefix, service.GetPrefix(null));
    }

    [Fact]
    public async Task LoadAllPrefixes_LoadsDatabaseGuildPrefixes()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Guilds.AddRange(
                new Guild { GuildId = 101, GuildName = "One", Prefix = "!", Leaderboard = true, Media = false, Tiktok = false },
                new Guild { GuildId = 102, GuildName = "Two", Prefix = "?", Leaderboard = true, Media = false, Tiktok = false });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = new PrefixService(factory);

        await service.LoadAllPrefixes();

        Assert.Equal("!", service.GetPrefix(101));
        Assert.Equal("?", service.GetPrefix(102));
    }

    [Fact]
    public async Task ReloadPrefix_UpdatesOrRemovesStoredPrefix()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Guilds.Add(new Guild { GuildId = 201, GuildName = "One", Prefix = "!", Leaderboard = true, Media = false, Tiktok = false });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = new PrefixService(factory);

        await service.ReloadPrefix(201);
        Assert.Equal("!", service.GetPrefix(201));

        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            var guild = await db.Guilds.SingleAsync(g => g.GuildId == 201, cancellationToken: TestContext.Current.CancellationToken);
            db.Guilds.Remove(guild);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await service.ReloadPrefix(201);

        Assert.Equal(Constants.StartPrefix, service.GetPrefix(201));
    }
}
