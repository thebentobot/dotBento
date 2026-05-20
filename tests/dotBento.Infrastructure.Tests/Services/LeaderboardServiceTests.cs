using dotBento.Domain.Enums.Leaderboard;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace dotBento.Infrastructure.Tests.Services;

public class LeaderboardServiceTests
{
    private sealed class InMemoryDbFactory : IDbContextFactory<BotDbContext>
    {
        private readonly string _dbName = Guid.NewGuid().ToString();
        private readonly InMemoryDatabaseRoot _root = new();
        private readonly IConfiguration _config = new ConfigurationBuilder().Build();

        public BotDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<BotDbContext>()
                .UseInMemoryDatabase(_dbName, _root)
                .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                .Options;
            return new BotDbContext(_config, options);
        }

        public Task<BotDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }

    private static async Task SeedUsersAsync(IDbContextFactory<BotDbContext> factory, int count)
    {
        await using var db = await factory.CreateDbContextAsync();
        for (var i = 0; i < count; i++)
        {
            db.Users.Add(new User
            {
                UserId = i + 1,
                Username = $"User{i + 1}",
                Discriminator = "0001",
                Level = count - i,
                Xp = (count - i) * 100
            });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedGuildMembersAsync(IDbContextFactory<BotDbContext> factory, long guildId, int count)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.Guilds.Add(new Guild
        {
            GuildId = guildId,
            GuildName = "TestGuild",
            Prefix = "!",
            Leaderboard = true,
            Media = false,
            Tiktok = false
        });
        for (var i = 0; i < count; i++)
        {
            db.Users.Add(new User
            {
                UserId = i + 100,
                Username = $"GuildUser{i + 1}",
                Discriminator = "0001",
                Level = count - i,
                Xp = (count - i) * 50
            });
            db.GuildMembers.Add(new GuildMember
            {
                GuildId = guildId,
                UserId = i + 100,
                Level = count - i,
                Xp = (count - i) * 50
            });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedBentosAsync(IDbContextFactory<BotDbContext> factory, int count)
    {
        await using var db = await factory.CreateDbContextAsync();
        for (var i = 0; i < count; i++)
        {
            db.Users.Add(new User
            {
                UserId = i + 200,
                Username = $"BentoUser{i + 1}",
                Discriminator = "0001",
                Level = 1,
                Xp = 0
            });
            db.Bentos.Add(new Bento
            {
                UserId = i + 200,
                Bento1 = (count - i) * 10,
                BentoDate = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedRpsGamesAsync(IDbContextFactory<BotDbContext> factory, int count)
    {
        await using var db = await factory.CreateDbContextAsync();
        for (var i = 0; i < count; i++)
        {
            db.Users.Add(new User
            {
                UserId = i + 300,
                Username = $"RpsUser{i + 1}",
                Discriminator = "0001",
                Level = 1,
                Xp = 0
            });
            db.RpsGames.Add(new RpsGame
            {
                UserId = i + 300,
                RockWins = (count - i) * 5,
                RockLosses = i,
                RockTies = 1,
                PaperWins = (count - i) * 3,
                PaperLosses = i + 1,
                PaperTies = 2,
                ScissorWins = (count - i) * 2,
                ScissorsLosses = i + 2,
                ScissorsTies = 1
            });
        }
        await db.SaveChangesAsync();
    }

    // --- Global XP ---

    [Fact]
    public async Task GetGlobalXpLeaderboardAsync_ReturnsUsersOrderedByLevelThenXp()
    {
        var factory = new InMemoryDbFactory();
        await SeedUsersAsync(factory, 5);
        var service = new LeaderboardService(factory);

        var result = await service.GetGlobalXpLeaderboardAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Count);
        Assert.Equal("User1", result.Value[0].Username);
        Assert.Equal(1, result.Value[0].Rank);
        Assert.Equal(5, result.Value[0].Level);
    }

    [Fact]
    public async Task GetGlobalXpLeaderboardAsync_RespectsLimit()
    {
        var factory = new InMemoryDbFactory();
        await SeedUsersAsync(factory, 10);
        var service = new LeaderboardService(factory);

        var result = await service.GetGlobalXpLeaderboardAsync(3);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
    }

    [Fact]
    public async Task GetGlobalXpLeaderboardAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var factory = new InMemoryDbFactory();
        var service = new LeaderboardService(factory);

        var result = await service.GetGlobalXpLeaderboardAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    // --- Server XP ---

    [Fact]
    public async Task GetServerXpLeaderboardAsync_ReturnsOnlyGuildMembers()
    {
        var factory = new InMemoryDbFactory();
        await SeedGuildMembersAsync(factory, 1, 5);
        await SeedUsersAsync(factory, 3); // these are not in guild 1
        var service = new LeaderboardService(factory);

        var result = await service.GetServerXpLeaderboardAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Count);
        Assert.All(result.Value, e => Assert.StartsWith("GuildUser", e.Username));
    }

    [Fact]
    public async Task GetServerXpLeaderboardAsync_NonexistentGuild_ReturnsEmptyList()
    {
        var factory = new InMemoryDbFactory();
        var service = new LeaderboardService(factory);

        var result = await service.GetServerXpLeaderboardAsync(999);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    // --- Global Bento ---

    [Fact]
    public async Task GetGlobalBentoLeaderboardAsync_ReturnsOrderedByBentoCount()
    {
        var factory = new InMemoryDbFactory();
        await SeedBentosAsync(factory, 5);
        var service = new LeaderboardService(factory);

        var result = await service.GetGlobalBentoLeaderboardAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Count);
        Assert.Equal("BentoUser1", result.Value[0].Username);
        Assert.Equal(50, result.Value[0].BentoCount);
        Assert.True(result.Value[0].BentoCount > result.Value[1].BentoCount);
    }

    // --- Server Bento ---

    [Fact]
    public async Task GetServerBentoLeaderboardAsync_ReturnsOnlyGuildMembersWithBento()
    {
        var factory = new InMemoryDbFactory();
        const long guildId = 10;

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Guilds.Add(new Guild { GuildId = guildId, GuildName = "BentoGuild", Prefix = "!", Leaderboard = true, Media = false, Tiktok = false });
            for (var i = 0; i < 3; i++)
            {
                db.Users.Add(new User { UserId = i + 500, Username = $"BG{i + 1}", Discriminator = "0", Level = 1, Xp = 0 });
                db.GuildMembers.Add(new GuildMember { GuildId = guildId, UserId = i + 500, Level = 1, Xp = 0 });
                db.Bentos.Add(new Bento { UserId = i + 500, Bento1 = (3 - i) * 20, BentoDate = DateTime.UtcNow });
            }
            await db.SaveChangesAsync();
        }

        var service = new LeaderboardService(factory);
        var result = await service.GetServerBentoLeaderboardAsync(guildId);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        Assert.Equal("BG1", result.Value[0].Username);
        Assert.Equal(60, result.Value[0].BentoCount);
    }

    // --- Global RPS ---

    [Fact]
    public async Task GetGlobalRpsLeaderboardAsync_AllType_ReturnsAggregatedStats()
    {
        var factory = new InMemoryDbFactory();
        await SeedRpsGamesAsync(factory, 5);
        var service = new LeaderboardService(factory);

        var result = await service.GetGlobalRpsLeaderboardAsync(RpsLeaderboardType.All, RpsLeaderboardOrder.Wins);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Count);
        // First user should have most total wins
        Assert.Equal("RpsUser1", result.Value[0].Username);
        // Total wins = RockWins + PaperWins + ScissorWins = 25 + 15 + 10 = 50
        Assert.Equal(50, result.Value[0].Wins);
    }

    [Fact]
    public async Task GetGlobalRpsLeaderboardAsync_RockType_ReturnsOnlyRockStats()
    {
        var factory = new InMemoryDbFactory();
        await SeedRpsGamesAsync(factory, 3);
        var service = new LeaderboardService(factory);

        var result = await service.GetGlobalRpsLeaderboardAsync(RpsLeaderboardType.Rock, RpsLeaderboardOrder.Wins);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        // RpsUser1: RockWins = 3*5 = 15, RpsUser2: RockWins = 2*5 = 10
        Assert.Equal(15, result.Value[0].Wins);
        Assert.Equal(10, result.Value[1].Wins);
    }

    [Fact]
    public async Task GetGlobalRpsLeaderboardAsync_OrderByLosses_SortsCorrectly()
    {
        var factory = new InMemoryDbFactory();
        await SeedRpsGamesAsync(factory, 3);
        var service = new LeaderboardService(factory);

        var result = await service.GetGlobalRpsLeaderboardAsync(RpsLeaderboardType.All, RpsLeaderboardOrder.Losses);

        Assert.True(result.IsSuccess);
        // User with most losses should be first (User3 has highest i values = most losses)
        Assert.Equal("RpsUser3", result.Value[0].Username);
    }

    // --- User Summary ---

    [Fact]
    public async Task GetUserLeaderboardSummaryAsync_ReturnsCorrectRanks()
    {
        var factory = new InMemoryDbFactory();
        const long guildId = 20;

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Guilds.Add(new Guild { GuildId = guildId, GuildName = "SummaryGuild", Prefix = "!", Leaderboard = true, Media = false, Tiktok = false });

            // Create 3 users with different levels
            for (var i = 0; i < 3; i++)
            {
                db.Users.Add(new User { UserId = i + 600, Username = $"SU{i + 1}", Discriminator = "0", Level = 3 - i, Xp = (3 - i) * 100 });
                db.GuildMembers.Add(new GuildMember { GuildId = guildId, UserId = i + 600, Level = 3 - i, Xp = (3 - i) * 100 });
                db.Bentos.Add(new Bento { UserId = i + 600, Bento1 = (3 - i) * 30, BentoDate = DateTime.UtcNow });
            }
            await db.SaveChangesAsync();
        }

        var service = new LeaderboardService(factory);

        // Check user 602 (lowest rank user: Level 1, Xp 100)
        var result = await service.GetUserLeaderboardSummaryAsync(602, guildId);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.GlobalXpRank);
        Assert.Equal(3, result.Value.ServerXpRank);
        Assert.Equal(1, result.Value.GlobalXpLevel);
        Assert.Equal(100, result.Value.GlobalXpXp);
        Assert.Equal(3, result.Value.BentoRank);
        Assert.Equal(30, result.Value.BentoCount);
        Assert.Equal(3, result.Value.ServerBentoRank);
    }

    [Fact]
    public async Task GetUserLeaderboardSummaryAsync_UserNotFound_ReturnsFailure()
    {
        var factory = new InMemoryDbFactory();
        var service = new LeaderboardService(factory);

        var result = await service.GetUserLeaderboardSummaryAsync(999, 1);

        Assert.True(result.IsFailure);
        Assert.Equal("User not found", result.Error);
    }

    [Fact]
    public async Task GetUserLeaderboardSummaryAsync_NotInGuild_ServerRankIsNull()
    {
        var factory = new InMemoryDbFactory();

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Users.Add(new User { UserId = 700, Username = "Loner", Discriminator = "0", Level = 5, Xp = 500 });
            await db.SaveChangesAsync();
        }

        var service = new LeaderboardService(factory);
        var result = await service.GetUserLeaderboardSummaryAsync(700, 1);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ServerXpRank);
        Assert.Equal(1, result.Value.GlobalXpRank);
    }
}
