using Discord;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace dotBento.Infrastructure.Tests.Services;

public class GuildServiceTests
{
    private static GuildService CreateService(
        InfrastructureTestDbFactory factory,
        IMemoryCache? cache = null) =>
        new(factory, cache ?? new MemoryCache(new MemoryCacheOptions()));

    private static Guild Guild(long id) => new()
    {
        GuildId = id,
        GuildName = $"Guild{id}",
        Prefix = "!",
        Leaderboard = true,
        Media = false,
        Tiktok = false,
        MemberCount = 3
    };

    private static User User(long id) => new()
    {
        UserId = id,
        Username = $"User{id}",
        Discriminator = "0001",
        Level = 1,
        Xp = 0
    };

    [Fact]
    public async Task GetGuildAndGuildMemberMethods_ReturnMaybeValues()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Guilds.Add(Guild(100));
            db.Users.Add(User(10));
            db.GuildMembers.Add(new GuildMember { GuildId = 100, UserId = 10, Level = 2, Xp = 20 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory);

        var guild = await service.GetGuildAsync(100);
        var guildMembers = await service.GetGuildUsers(100);
        var guildMember = await service.GetGuildMemberAsync(100, 10);
        var missingMember = await service.GetGuildMemberAsync(100, 999);

        Assert.True(guild.HasValue);
        Assert.True(guildMembers.HasValue);
        Assert.Contains(10, guildMembers.Value.Keys);
        Assert.True(guildMember.HasValue);
        Assert.True(missingMember.HasNoValue);
    }

    [Fact]
    public async Task RemoveGuildAsync_RemovesExistingGuild()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Guilds.Add(Guild(100));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory);

        await service.RemoveGuildAsync(100);
        await service.RemoveGuildAsync(999);

        await using var assertDb = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Empty(assertDb.Guilds);
    }

    [Fact]
    public async Task UpdateGuildPrefixAsync_UpdatesExistingAndReturnsNoneForMissing()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Guilds.Add(Guild(100));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory);

        var updated = await service.UpdateGuildPrefixAsync(100, "?");
        var missing = await service.UpdateGuildPrefixAsync(999, "?");

        Assert.True(updated.HasValue);
        Assert.Equal("?", updated.Value.Prefix);
        Assert.True(missing.HasNoValue);
    }

    [Fact]
    public async Task DeleteGuildMember_RemovesExistingMember()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Guilds.Add(Guild(100));
            db.Users.Add(User(10));
            db.GuildMembers.Add(new GuildMember { GuildId = 100, UserId = 10, Level = 1, Xp = 0 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory);

        await service.DeleteGuildMember(100, 10);
        await service.DeleteGuildMember(100, 999);

        await using var assertDb = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Empty(assertDb.GuildMembers);
    }

    [Fact]
    public async Task CountsBatchesAndRanks_ReturnExpectedValues()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Guilds.AddRange(Guild(100), Guild(200), Guild(300));
            db.Users.AddRange(User(1), User(2), User(3));
            db.GuildMembers.AddRange(
                new GuildMember { GuildId = 100, UserId = 1, Level = 3, Xp = 0 },
                new GuildMember { GuildId = 100, UserId = 2, Level = 2, Xp = 50 },
                new GuildMember { GuildId = 100, UserId = 3, Level = 2, Xp = 10 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory);

        var guildCount = await service.GetTotalGuildCountAsync();
        var guildBatch = await service.GetGuildBatchAsync(batchSize: 2, skip: 1);
        var memberBatch = await service.GetGuildMemberBatchAsync(batchSize: 2, skip: 1);
        var rank = await service.GetGuildMemberRankAsync(3, 100);
        var missingRank = await service.GetGuildMemberRankAsync(999, 100);

        Assert.Equal(3, guildCount);
        Assert.Equal([200L, 300L], guildBatch.Select(g => g.GuildId));
        Assert.Equal(2, memberBatch.Count);
        Assert.True(rank.HasValue);
        Assert.Equal(3, rank.Value);
        Assert.True(missingRank.HasNoValue);
    }

    [Fact]
    public async Task UpdateGuildMemberCountAsync_UpdatesExistingGuildOnly()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Guilds.Add(Guild(100));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory);

        await service.UpdateGuildMemberCountAsync(100, 42);
        await service.UpdateGuildMemberCountAsync(999, 1);

        await using var assertDb = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal(42, (await assertDb.Guilds.SingleAsync(cancellationToken: TestContext.Current.CancellationToken)).MemberCount);
    }

    [Fact]
    public async Task DeleteGuildMembersBulkAsync_RemovesSelectedMembers()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Guilds.Add(Guild(100));
            db.Users.AddRange(User(1), User(2), User(3));
            db.GuildMembers.AddRange(
                new GuildMember { GuildMemberId = 1, GuildId = 100, UserId = 1, Level = 1, Xp = 0 },
                new GuildMember { GuildMemberId = 2, GuildId = 100, UserId = 2, Level = 1, Xp = 0 },
                new GuildMember { GuildMemberId = 3, GuildId = 100, UserId = 3, Level = 1, Xp = 0 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = CreateService(factory);

        await service.DeleteGuildMembersBulkAsync([1, 3]);
        await service.DeleteGuildMembersBulkAsync([]);

        await using var assertDb = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var remainingIds = await assertDb.GuildMembers
            .Select(gm => gm.GuildMemberId)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal([2], remainingIds);
    }

    [Fact]
    public async Task SyncGuildFromDiscordAsync_UpdatesChangedFields()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Guilds.Add(Guild(100));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var discordGuild = new Mock<IGuild>();
        discordGuild.SetupGet(g => g.Name).Returns("Updated");
        discordGuild.SetupGet(g => g.IconUrl).Returns("https://cdn.example.com/icon.png");
        var service = CreateService(factory);

        var changed = await service.SyncGuildFromDiscordAsync(Guild(100), discordGuild.Object);
        var missing = await service.SyncGuildFromDiscordAsync(Guild(999), discordGuild.Object);

        Assert.True(changed);
        Assert.False(missing);
        await using var assertDb = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var guild = await assertDb.Guilds.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Updated", guild.GuildName);
        Assert.Equal("https://cdn.example.com/icon.png", guild.Icon);
    }
}
