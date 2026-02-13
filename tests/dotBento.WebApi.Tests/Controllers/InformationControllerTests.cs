using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using dotBento.Infrastructure.Services.Api;
using dotBento.WebApi.Controllers;
using dotBento.WebApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace dotBento.WebApi.Tests.Controllers;

public class InformationControllerTests
{
    private sealed class SingleContextFactory(BotDbContext ctx) : IDbContextFactory<BotDbContext>
    {
        public BotDbContext CreateDbContext() => CreateNewContextSharingStore();

        public Task<BotDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateNewContextSharingStore());

        private BotDbContext CreateNewContextSharingStore()
        {
            if (ctx is TestBotDbContext tctx)
            {
                var configuration = new ConfigurationBuilder().Build();
                var newOptions = new DbContextOptionsBuilder<BotDbContext>()
                    .UseInMemoryDatabase(tctx.DatabaseName, tctx.Root)
                    .Options;
                return new BotDbContext(configuration, newOptions);
            }

            throw new InvalidOperationException("Expected TestBotDbContext for in-memory testing.");
        }
    }

    private static InformationController CreateController(BotDbContext context) =>
        CreateController(context, CreateMockDiscordApiService().Object);

    private static InformationController CreateController(
        BotDbContext context, DiscordApiService discordApiService)
    {
        var factory = new SingleContextFactory(context);
        var leaderboardService = new LeaderboardService(factory);
        return new InformationController(
            Mock.Of<ILogger<InformationController>>(),
            context,
            leaderboardService,
            discordApiService);
    }

    private static Mock<DiscordApiService> CreateMockDiscordApiService(
        bool isMember = false, string? failure = null)
    {
        var mock = new Mock<DiscordApiService>(new HttpClient()) { CallBase = false };
        if (failure != null)
        {
            mock.Setup(x => x.GetGuildMemberAsync(It.IsAny<ulong>(), It.IsAny<ulong>()))
                .ReturnsAsync(Result.Failure<bool>(failure));
        }
        else
        {
            mock.Setup(x => x.GetGuildMemberAsync(It.IsAny<ulong>(), It.IsAny<ulong>()))
                .ReturnsAsync(Result.Success(isMember));
        }
        return mock;
    }

    private static void SeedGuildWithMembers(BotDbContext context, long guildId, long[] memberUserIds)
    {
        context.Guilds.Add(new Guild
        {
            GuildId = guildId,
            GuildName = "TestGuild",
            Prefix = "!",
            Leaderboard = true,
            Media = false,
            Tiktok = false,
            MemberCount = memberUserIds.Length,
            Icon = "https://cdn.example.com/icon.png"
        });
        foreach (var uid in memberUserIds)
        {
            context.Users.Add(new User
            {
                UserId = uid,
                Username = $"User{uid}",
                Discriminator = "0001"
            });
            context.GuildMembers.Add(new GuildMember
            {
                GuildId = guildId,
                UserId = uid,
                Level = (int)uid,
                Xp = 100 * (int)uid
            });
        }
    }

    [Fact]
    public async Task GetUsageStats_ReturnsCorrectStats()
    {
        // Arrange
        await using var context = DbContextHelper.GetInMemoryDbContext();
        context.Guilds.AddRange(
            new Guild
            {
                GuildId = 1,
                GuildName = "Guild One",
                Prefix = "!",
                Tiktok = false,
                Leaderboard = true,
                Media = true,
                MemberCount = 10
            },
            new Guild
            {
                GuildId = 2,
                GuildName = "Guild Two",
                Prefix = "?",
                Tiktok = true,
                Leaderboard = false,
                Media = false,
                MemberCount = 20
            }
        );
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CreateController(context);

        // Act
        var result = await controller.GetUsageStats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stats = Assert.IsType<UsageStatsDto>(okResult.Value);
        Assert.Equal(30, stats.UserCount);
        Assert.Equal(2, stats.ServerCount);
    }

    [Fact]
    public async Task GetPatreon_ReturnsCorrectPatreonUsers()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        context.Patreons.AddRange(
            new Patreon { UserId = 1, Name = "Alice", Avatar = "alice.png", Supporter = true, Follower = true },
            new Patreon { UserId = 2, Name = "Bob", Avatar = "bob.png", Sponsor = true, Enthusiast = true }
        );
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CreateController(context);

        var result = await controller.GetPatreon();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var patreonList = Assert.IsAssignableFrom<IEnumerable<PatreonUserDto>>(okResult.Value);

        Assert.Collection(patreonList,
            p => Assert.Equal("Alice", p.Name),
            p => Assert.Equal("Bob", p.Name)
        );
    }

    [Fact]
    public async Task GetPatreon_WhenEmpty_ReturnsEmptyList()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);

        var result = await controller.GetPatreon();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var patreonList = Assert.IsAssignableFrom<IEnumerable<PatreonUserDto>>(okResult.Value);
        Assert.Empty(patreonList);
    }

    [Fact]
    public async Task GetLeaderboard_Global_ReturnsTop50()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        for (int i = 0; i < 100; i++)
        {
            context.Users.Add(new User
            {
                UserId = i,
                Username = $"User{i}",
                Discriminator = "0001",
                Level = i % 10,
                Xp = 1000 - i
            });
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CreateController(context);

        var result = await controller.GetLeaderboard();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var leaderboardResponse = Assert.IsType<LeaderboardResponseDto>(okResult.Value);

        Assert.Null(leaderboardResponse.GuildName);
        Assert.Null(leaderboardResponse.Icon);
        Assert.NotNull(leaderboardResponse.Users);
        Assert.Equal(50, leaderboardResponse.Users.Count);

        Assert.Equal("User9", leaderboardResponse.Users.First().Username); // Highest level + XP
    }

    [Fact]
    public async Task GetLeaderboardWithAccess_GuildMemberInDb_ReturnsLeaderboard()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        SeedGuildWithMembers(context, guildId: 100, memberUserIds: [1, 2, 3]);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CreateController(context, CreateMockDiscordApiService().Object);

        var result = await controller.GetLeaderboardWithAccess("100", "1");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<LeaderboardResponseDto>(okResult.Value);
        Assert.Equal("TestGuild", dto.GuildName);
        Assert.Equal(3, dto.Users.Count);
    }

    [Fact]
    public async Task GetLeaderboardWithAccess_UserNotInBotDb_Returns403NotBotUser()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        SeedGuildWithMembers(context, guildId: 100, memberUserIds: [1, 2]);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CreateController(context, CreateMockDiscordApiService().Object);

        var result = await controller.GetLeaderboardWithAccess("100", "999");

        var forbidResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, forbidResult.StatusCode);
        var dto = Assert.IsType<LeaderboardAccessDeniedDto>(forbidResult.Value);
        Assert.Equal("not_bot_user", dto.Reason);
    }

    [Fact]
    public async Task GetLeaderboardWithAccess_NotGuildMember_DiscordConfirms_ReturnsLeaderboard()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        SeedGuildWithMembers(context, guildId: 100, memberUserIds: [1, 2]);
        // User 50 exists in bot DB but is NOT in guild 100
        context.Users.Add(new User { UserId = 50, Username = "OutsideUser", Discriminator = "0001" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockDiscord = CreateMockDiscordApiService(isMember: true);
        var controller = CreateController(context, mockDiscord.Object);

        var result = await controller.GetLeaderboardWithAccess("100", "50");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<LeaderboardResponseDto>(okResult.Value);
        Assert.Equal("TestGuild", dto.GuildName);
    }

    [Fact]
    public async Task GetLeaderboardWithAccess_NotGuildMember_DiscordDenies_Returns403NotMember()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        SeedGuildWithMembers(context, guildId: 100, memberUserIds: [1, 2]);
        context.Users.Add(new User { UserId = 50, Username = "OutsideUser", Discriminator = "0001" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockDiscord = CreateMockDiscordApiService(isMember: false);
        var controller = CreateController(context, mockDiscord.Object);

        var result = await controller.GetLeaderboardWithAccess("100", "50");

        var forbidResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, forbidResult.StatusCode);
        var dto = Assert.IsType<LeaderboardAccessDeniedDto>(forbidResult.Value);
        Assert.Equal("not_member", dto.Reason);
    }

    [Fact]
    public async Task GetLeaderboardWithAccess_DiscordApiError_Returns502()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        SeedGuildWithMembers(context, guildId: 100, memberUserIds: [1]);
        context.Users.Add(new User { UserId = 50, Username = "OutsideUser", Discriminator = "0001" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockDiscord = CreateMockDiscordApiService(failure: "Discord API returned 500");
        var controller = CreateController(context, mockDiscord.Object);

        var result = await controller.GetLeaderboardWithAccess("100", "50");

        var errorResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(502, errorResult.StatusCode);
    }

    [Fact]
    public async Task GetLeaderboardWithAccess_InvalidGuildId_ReturnsBadRequest()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context, CreateMockDiscordApiService().Object);

        var result = await controller.GetLeaderboardWithAccess("not_a_number", "1");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetLeaderboardWithAccess_InvalidUserId_ReturnsBadRequest()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context, CreateMockDiscordApiService().Object);

        var result = await controller.GetLeaderboardWithAccess("100", "not_a_number");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetLeaderboardWithAccess_GuildNotFound_ReturnsNotFound()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        context.Users.Add(new User { UserId = 1, Username = "User1", Discriminator = "0001" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var controller = CreateController(context, CreateMockDiscordApiService().Object);

        var result = await controller.GetLeaderboardWithAccess("999", "1");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
