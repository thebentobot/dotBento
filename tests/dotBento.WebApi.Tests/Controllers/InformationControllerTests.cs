using dotBento.EntityFramework.Entities;
using dotBento.WebApi.Controllers;
using dotBento.WebApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace dotBento.WebApi.Tests.Controllers;

public class InformationControllerTests
{
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
        await context.SaveChangesAsync();

        var logger = new Mock<ILogger<InformationController>>();
        var controller = new InformationController(logger.Object, context);

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
        await context.SaveChangesAsync();

        var controller = new InformationController(Mock.Of<ILogger<InformationController>>(), context);

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
        var controller = new InformationController(Mock.Of<ILogger<InformationController>>(), context);

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
        await context.SaveChangesAsync();

        var controller = new InformationController(Mock.Of<ILogger<InformationController>>(), context);

        var result = await controller.GetLeaderboard(null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var leaderboardResponse = Assert.IsType<LeaderboardResponseDto>(okResult.Value);

        Assert.Null(leaderboardResponse.GuildName);
        Assert.Null(leaderboardResponse.Icon);
        Assert.NotNull(leaderboardResponse.Users);
        Assert.Equal(50, leaderboardResponse.Users.Count);

        Assert.Equal("User9", leaderboardResponse.Users.First().Username); // Highest level + XP
    }

    [Fact]
    public async Task GetLeaderboard_Server_ReturnsTop50WithGuildData()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        context.Guilds.Add(new Guild
        {
            GuildId = 1,
            GuildName = "TestGuild",
            Prefix = "!",
            Leaderboard = true,
            Media = false,
            Tiktok = false,
            MemberCount = 100,
            Icon = "https://cdn.example.com/icon.png"
        });

        for (int i = 0; i < 100; i++)
        {
            context.Users.Add(new User
            {
                UserId = i,
                Username = $"User{i}",
                Discriminator = "0001"
            });
            context.GuildMembers.Add(new GuildMember
            {
                GuildId = 1,
                UserId = i,
                Level = i % 10,
                Xp = 1000 - i,
                AvatarUrl = $"avatar{i}"
            });
        }
        await context.SaveChangesAsync();

        var controller = new InformationController(Mock.Of<ILogger<InformationController>>(), context);

        var result = await controller.GetLeaderboard("1");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var leaderboardResponse = Assert.IsType<LeaderboardResponseDto>(okResult.Value);

        Assert.Equal("TestGuild", leaderboardResponse.GuildName);
        Assert.Equal("https://cdn.example.com/icon.png", leaderboardResponse.Icon);
        Assert.NotNull(leaderboardResponse.Users);
        Assert.Equal(50, leaderboardResponse.Users.Count);

        Assert.Equal("User9", leaderboardResponse.Users.First().Username); // Highest level + XP
    }

    [Fact]
    public async Task GetLeaderboard_InvalidGuildId_ReturnsBadRequest()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = new InformationController(Mock.Of<ILogger<InformationController>>(), context);

        var result = await controller.GetLeaderboard("invalid_id");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid guild ID", badRequest.Value);
    }
}
