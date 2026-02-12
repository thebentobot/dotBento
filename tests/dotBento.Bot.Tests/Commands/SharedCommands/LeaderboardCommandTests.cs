using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Resources;
using dotBento.Domain.Enums.Leaderboard;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace dotBento.Bot.Tests.Commands.SharedCommands;

public class LeaderboardCommandTests
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

    private static async Task<(LeaderboardCommand Command, InMemoryDbFactory Factory)> CreateCommandWithUsersAsync(int userCount)
    {
        var factory = new InMemoryDbFactory();
        await using var db = await factory.CreateDbContextAsync();
        for (var i = 0; i < userCount; i++)
        {
            db.Users.Add(new User
            {
                UserId = i + 1,
                Username = $"User{i + 1}",
                Discriminator = "0001",
                Level = userCount - i,
                Xp = (userCount - i) * 100
            });
        }
        await db.SaveChangesAsync();

        var service = new LeaderboardService(factory);
        var command = new LeaderboardCommand(service);
        return (command, factory);
    }

    private static async Task<LeaderboardCommand> CreateCommandWithGuildMembersAsync(InMemoryDbFactory factory, long guildId, int count)
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

        return new LeaderboardCommand(new LeaderboardService(factory));
    }

    // --- Server XP Leaderboard ---

    [Fact]
    public async Task GetServerXpLeaderboardAsync_Success_ReturnsPaginator()
    {
        var factory = new InMemoryDbFactory();
        var command = await CreateCommandWithGuildMembersAsync(factory, 1, 3);

        var result = await command.GetServerXpLeaderboardAsync(1, "TestServer", "https://icon.url");

        Assert.Equal(ResponseType.Paginator, result.ResponseType);
        Assert.NotNull(result.StaticPaginator);
    }

    [Fact]
    public async Task GetServerXpLeaderboardAsync_Title_ContainsServerName()
    {
        var factory = new InMemoryDbFactory();
        var command = await CreateCommandWithGuildMembersAsync(factory, 1, 3);

        var result = await command.GetServerXpLeaderboardAsync(1, "MyServer", "https://icon.url");

        Assert.Equal(ResponseType.Paginator, result.ResponseType);
        var pages = result.StaticPaginator!.Pages.ToList();
        Assert.Single(pages);
        var embed = pages[0].GetEmbedArray()[0];
        Assert.Equal("Leaderboard for MyServer", embed.Title);
    }

    [Fact]
    public async Task GetServerXpLeaderboardAsync_EmptyResult_ReturnsErrorEmbed()
    {
        var factory = new InMemoryDbFactory();
        var service = new LeaderboardService(factory);
        var command = new LeaderboardCommand(service);

        var result = await command.GetServerXpLeaderboardAsync(999, "TestServer", null);

        Assert.Equal(ResponseType.Embed, result.ResponseType);
        Assert.Equal("No users found on the server leaderboard.", result.Embed.Title);
    }

    // --- Global XP Leaderboard ---

    [Fact]
    public async Task GetGlobalXpLeaderboardAsync_Success_ReturnsGlobalLeaderboardTitle()
    {
        var (command, _) = await CreateCommandWithUsersAsync(5);

        var result = await command.GetGlobalXpLeaderboardAsync("https://bot.avatar");

        Assert.Equal(ResponseType.Paginator, result.ResponseType);
        var pages = result.StaticPaginator!.Pages.ToList();
        var embed = pages[0].GetEmbedArray()[0];
        Assert.Equal("Global Leaderboard", embed.Title);
    }

    [Fact]
    public async Task GetGlobalXpLeaderboardAsync_UsesBotAvatarAsThumbnail()
    {
        var (command, _) = await CreateCommandWithUsersAsync(1);

        var result = await command.GetGlobalXpLeaderboardAsync("https://bot.avatar");

        var pages = result.StaticPaginator!.Pages.ToList();
        var embed = pages[0].GetEmbedArray()[0];
        Assert.Equal("https://bot.avatar", embed.Thumbnail!.Value.Url);
    }

    // --- Server Bento Leaderboard ---

    [Fact]
    public async Task GetServerBentoLeaderboardAsync_Success_ReturnsBentoLeaderboardTitle()
    {
        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Guilds.Add(new Guild { GuildId = 1, GuildName = "BentoGuild", Prefix = "!", Leaderboard = true, Media = false, Tiktok = false });
            for (var i = 0; i < 3; i++)
            {
                db.Users.Add(new User { UserId = i + 1, Username = $"BU{i + 1}", Discriminator = "0001", Level = 1, Xp = 0 });
                db.GuildMembers.Add(new GuildMember { GuildId = 1, UserId = i + 1, Level = 1, Xp = 0 });
                db.Bentos.Add(new Bento { UserId = i + 1, Bento1 = (3 - i) * 10, BentoDate = DateTime.UtcNow });
            }
            await db.SaveChangesAsync();
        }

        var command = new LeaderboardCommand(new LeaderboardService(factory));
        var result = await command.GetServerBentoLeaderboardAsync(1, "BentoServer", "https://icon.url");

        Assert.Equal(ResponseType.Paginator, result.ResponseType);
        var pages = result.StaticPaginator!.Pages.ToList();
        var embed = pages[0].GetEmbedArray()[0];
        Assert.Equal("Bento Leaderboard for BentoServer", embed.Title);
    }

    // --- Global Bento Leaderboard ---

    [Fact]
    public async Task GetGlobalBentoLeaderboardAsync_Success_ReturnsGlobalBentoTitle()
    {
        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            for (var i = 0; i < 3; i++)
            {
                db.Users.Add(new User { UserId = i + 1, Username = $"BU{i + 1}", Discriminator = "0001", Level = 1, Xp = 0 });
                db.Bentos.Add(new Bento { UserId = i + 1, Bento1 = (3 - i) * 10, BentoDate = DateTime.UtcNow });
            }
            await db.SaveChangesAsync();
        }

        var command = new LeaderboardCommand(new LeaderboardService(factory));
        var result = await command.GetGlobalBentoLeaderboardAsync("https://bot.avatar");

        Assert.Equal(ResponseType.Paginator, result.ResponseType);
        var pages = result.StaticPaginator!.Pages.ToList();
        var embed = pages[0].GetEmbedArray()[0];
        Assert.Equal("Global Bento Leaderboard", embed.Title);
    }

    // --- Server RPS Leaderboard ---

    [Fact]
    public async Task GetServerRpsLeaderboardAsync_AllType_TitleWithoutTypeLabel()
    {
        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Guilds.Add(new Guild { GuildId = 1, GuildName = "RG", Prefix = "!", Leaderboard = true, Media = false, Tiktok = false });
            for (var i = 0; i < 3; i++)
            {
                db.Users.Add(new User { UserId = i + 1, Username = $"RU{i + 1}", Discriminator = "0001", Level = 1, Xp = 0 });
                db.GuildMembers.Add(new GuildMember { GuildId = 1, UserId = i + 1, Level = 1, Xp = 0 });
                db.RpsGames.Add(new RpsGame { UserId = i + 1, RockWins = 5, RockLosses = 1, RockTies = 1 });
            }
            await db.SaveChangesAsync();
        }

        var command = new LeaderboardCommand(new LeaderboardService(factory));
        var result = await command.GetServerRpsLeaderboardAsync(1, "RpsServer", null, RpsLeaderboardType.All, RpsLeaderboardOrder.Wins);

        var pages = result.StaticPaginator!.Pages.ToList();
        var embed = pages[0].GetEmbedArray()[0];
        Assert.Equal("RPS Leaderboard for RpsServer", embed.Title);
    }

    [Fact]
    public async Task GetServerRpsLeaderboardAsync_RockType_TitleIncludesTypeLabel()
    {
        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Guilds.Add(new Guild { GuildId = 1, GuildName = "RG", Prefix = "!", Leaderboard = true, Media = false, Tiktok = false });
            for (var i = 0; i < 2; i++)
            {
                db.Users.Add(new User { UserId = i + 1, Username = $"RU{i + 1}", Discriminator = "0001", Level = 1, Xp = 0 });
                db.GuildMembers.Add(new GuildMember { GuildId = 1, UserId = i + 1, Level = 1, Xp = 0 });
                db.RpsGames.Add(new RpsGame { UserId = i + 1, RockWins = 5, RockLosses = 1, RockTies = 1 });
            }
            await db.SaveChangesAsync();
        }

        var command = new LeaderboardCommand(new LeaderboardService(factory));
        var result = await command.GetServerRpsLeaderboardAsync(1, "RpsServer", null, RpsLeaderboardType.Rock, RpsLeaderboardOrder.Wins);

        var pages = result.StaticPaginator!.Pages.ToList();
        var embed = pages[0].GetEmbedArray()[0];
        Assert.Equal("RPS Leaderboard for RpsServer (Rock)", embed.Title);
    }

    // --- Global RPS Leaderboard ---

    [Fact]
    public async Task GetGlobalRpsLeaderboardAsync_Success_ReturnsGlobalRpsTitle()
    {
        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            for (var i = 0; i < 3; i++)
            {
                db.Users.Add(new User { UserId = i + 1, Username = $"RU{i + 1}", Discriminator = "0001", Level = 1, Xp = 0 });
                db.RpsGames.Add(new RpsGame { UserId = i + 1, RockWins = 5, RockLosses = 1, RockTies = 1 });
            }
            await db.SaveChangesAsync();
        }

        var command = new LeaderboardCommand(new LeaderboardService(factory));
        var result = await command.GetGlobalRpsLeaderboardAsync(RpsLeaderboardType.All, RpsLeaderboardOrder.Wins, "https://bot.avatar");

        var pages = result.StaticPaginator!.Pages.ToList();
        var embed = pages[0].GetEmbedArray()[0];
        Assert.Equal("Global RPS Leaderboard", embed.Title);
    }

    // --- User Summary ---

    [Fact]
    public async Task GetUserSummaryAsync_Success_ReturnsEmbedWithCorrectAuthor()
    {
        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Guilds.Add(new Guild { GuildId = 1, GuildName = "TestGuild", Prefix = "!", Leaderboard = true, Media = false, Tiktok = false });
            db.Users.Add(new User { UserId = 1, Username = "TestUser", Discriminator = "0001", Level = 5, Xp = 500 });
            db.GuildMembers.Add(new GuildMember { GuildId = 1, UserId = 1, Level = 5, Xp = 500 });
            db.Bentos.Add(new Bento { UserId = 1, Bento1 = 42, BentoDate = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        var command = new LeaderboardCommand(new LeaderboardService(factory));
        var result = await command.GetUserSummaryAsync(1, 1, "TestUser", "https://avatar.url", "TestGuild");

        Assert.Equal(ResponseType.Embed, result.ResponseType);
        var builtEmbed = result.Embed.Build();
        Assert.Equal("TestUser's Rankings", builtEmbed.Author!.Value.Name);
        Assert.Equal("https://avatar.url", builtEmbed.Author!.Value.IconUrl);
        Assert.Equal("https://avatar.url", builtEmbed.Thumbnail!.Value.Url);
        Assert.Equal(DiscordConstants.BentoYellow, builtEmbed.Color!.Value);
    }

    [Fact]
    public async Task GetUserSummaryAsync_DescriptionContainsGuildNameForServerRank()
    {
        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Guilds.Add(new Guild { GuildId = 1, GuildName = "CoolGuild", Prefix = "!", Leaderboard = true, Media = false, Tiktok = false });
            for (var i = 0; i < 3; i++)
            {
                db.Users.Add(new User { UserId = i + 1, Username = $"U{i + 1}", Discriminator = "0001", Level = 3 - i, Xp = (3 - i) * 100 });
                db.GuildMembers.Add(new GuildMember { GuildId = 1, UserId = i + 1, Level = 3 - i, Xp = (3 - i) * 100 });
                db.Bentos.Add(new Bento { UserId = i + 1, Bento1 = (3 - i) * 30, BentoDate = DateTime.UtcNow });
            }
            await db.SaveChangesAsync();
        }

        var command = new LeaderboardCommand(new LeaderboardService(factory));
        // User 2 is rank #2 in the guild
        var result = await command.GetUserSummaryAsync(2, 1, "TestUser", "https://avatar.url", "CoolGuild");

        var builtEmbed = result.Embed.Build();
        Assert.Contains("**CoolGuild Rank:** #2", builtEmbed.Description);
        Assert.Contains("**Global Rank:** #2", builtEmbed.Description);
        Assert.Contains("**CoolGuild Bento Rank:** #2", builtEmbed.Description);
        Assert.Contains("**Global Bento Rank:** #2", builtEmbed.Description);
    }

    [Fact]
    public async Task GetUserSummaryAsync_NoServerRank_OmitsServerLine()
    {
        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Users.Add(new User { UserId = 1, Username = "Loner", Discriminator = "0001", Level = 3, Xp = 300 });
            await db.SaveChangesAsync();
        }

        var command = new LeaderboardCommand(new LeaderboardService(factory));
        var result = await command.GetUserSummaryAsync(1, 999, "TestUser", "https://avatar.url", "TestGuild");

        var builtEmbed = result.Embed.Build();
        Assert.DoesNotContain("TestGuild Rank:", builtEmbed.Description);
        Assert.Contains("**Global Rank:** #1", builtEmbed.Description);
        Assert.DoesNotContain("Bento Rank:", builtEmbed.Description);
    }

    [Fact]
    public async Task GetUserSummaryAsync_Failure_ReturnsErrorEmbed()
    {
        var factory = new InMemoryDbFactory();
        var command = new LeaderboardCommand(new LeaderboardService(factory));

        var result = await command.GetUserSummaryAsync(999, 1, "Nobody", "https://avatar.url", "TestGuild");

        Assert.Equal(ResponseType.Embed, result.ResponseType);
        Assert.Equal("User not found", result.Embed.Title);
    }

    // --- Pagination ---

    [Fact]
    public async Task GetGlobalXpLeaderboardAsync_MoreThan10Entries_CreatesMultiplePages()
    {
        var (command, _) = await CreateCommandWithUsersAsync(25);

        var result = await command.GetGlobalXpLeaderboardAsync("https://bot.avatar");

        Assert.Equal(ResponseType.Paginator, result.ResponseType);
        var pages = result.StaticPaginator!.Pages.ToList();
        Assert.Equal(3, pages.Count); // 25 entries / 10 per page = 3 pages
    }

    [Fact]
    public async Task GetGlobalXpLeaderboardAsync_Exactly10Entries_CreatesSinglePage()
    {
        var (command, _) = await CreateCommandWithUsersAsync(10);

        var result = await command.GetGlobalXpLeaderboardAsync("https://bot.avatar");

        var pages = result.StaticPaginator!.Pages.ToList();
        Assert.Single(pages);
    }

    // --- Entry Formatting ---

    [Fact]
    public async Task GetGlobalXpLeaderboardAsync_PageDescriptionContainsUserData()
    {
        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Users.Add(new User { UserId = 1, Username = "TopPlayer", Discriminator = "0001", Level = 10, Xp = 1000 });
            db.Users.Add(new User { UserId = 2, Username = "SecondPlayer", Discriminator = "0001", Level = 8, Xp = 800 });
            await db.SaveChangesAsync();
        }

        var command = new LeaderboardCommand(new LeaderboardService(factory));
        var result = await command.GetGlobalXpLeaderboardAsync(null);

        var embed = result.StaticPaginator!.Pages.First().GetEmbedArray()[0];
        Assert.Contains("**1.** TopPlayer", embed.Description);
        Assert.Contains("Level 10 (1000 XP)", embed.Description);
        Assert.Contains("**2.** SecondPlayer", embed.Description);
    }

    [Fact]
    public async Task GetGlobalBentoLeaderboardAsync_PageDescriptionContainsBentoData()
    {
        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Users.Add(new User { UserId = 1, Username = "BentoKing", Discriminator = "0001", Level = 1, Xp = 0 });
            db.Bentos.Add(new Bento { UserId = 1, Bento1 = 100, BentoDate = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        var command = new LeaderboardCommand(new LeaderboardService(factory));
        var result = await command.GetGlobalBentoLeaderboardAsync(null);

        var embed = result.StaticPaginator!.Pages.First().GetEmbedArray()[0];
        Assert.Contains("100 Bento", embed.Description);
        Assert.Contains("BentoKing", embed.Description);
    }

    [Fact]
    public async Task GetGlobalRpsLeaderboardAsync_PageDescriptionContainsWinRate()
    {
        var factory = new InMemoryDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Users.Add(new User { UserId = 1, Username = "RpsChamp", Discriminator = "0001", Level = 1, Xp = 0 });
            db.RpsGames.Add(new RpsGame
            {
                UserId = 1,
                RockWins = 7, RockTies = 2, RockLosses = 1,
                PaperWins = 0, PaperTies = 0, PaperLosses = 0,
                ScissorWins = 0, ScissorsTies = 0, ScissorsLosses = 0
            });
            await db.SaveChangesAsync();
        }

        var command = new LeaderboardCommand(new LeaderboardService(factory));
        var result = await command.GetGlobalRpsLeaderboardAsync(RpsLeaderboardType.All, RpsLeaderboardOrder.Wins, null);

        var embed = result.StaticPaginator!.Pages.First().GetEmbedArray()[0];
        // 7 wins out of 10 total = 70%
        Assert.Contains("7W / 2T / 1L (70%)", embed.Description);
    }
}
