﻿using dotBento.EntityFramework.Entities;
using dotBento.WebApi.Controllers;
using dotBento.WebApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace dotBento.WebApi.Tests;

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
        // Arrange
        await using var context = DbContextHelper.GetInMemoryDbContext();
        context.Patreons.AddRange(
            new Patreon { UserId = 1, Name = "Alice", Supporter = true, Follower = true, Enthusiast = false, Disciple = false, Sponsor = false },
            new Patreon { UserId = 2, Name = "Bob", Supporter = true, Follower = false, Enthusiast = true, Disciple = false, Sponsor = true }
        );
        await context.SaveChangesAsync();

        var controller = new InformationController(Mock.Of<ILogger<InformationController>>(), context);

        // Act
        var result = await controller.GetPatreon();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var patreonList = Assert.IsAssignableFrom<IEnumerable<PatreonUserDto>>(okResult.Value);
        var patreonUserDtos = patreonList as PatreonUserDto[] ?? patreonList.ToArray();
        Assert.Equal(2, patreonUserDtos.Count());
        Assert.Contains(patreonUserDtos, p => p.Name == "Alice");
        Assert.Contains(patreonUserDtos, p => p.Name == "Bob");
    }
    
    [Fact]
    public async Task GetPatreon_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = new InformationController(Mock.Of<ILogger<InformationController>>(), context);

        // Act
        var result = await controller.GetPatreon();

        // Assert
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
                Level = i % 10,
                Xp = 1000 - i,
                Username = $"User{i}",
                Discriminator = "0001"
            });
        }

        await context.SaveChangesAsync();

        var logger = new Mock<ILogger<InformationController>>();
        var controller = new InformationController(logger.Object, context);

        var result = await controller.GetLeaderboard(null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var leaderboard = Assert.IsAssignableFrom<IEnumerable<LeaderboardUserDto>>(ok.Value);
        var leaderboardUserDtos = leaderboard as LeaderboardUserDto[] ?? leaderboard.ToArray();
        Assert.Equal(50, leaderboardUserDtos.Count());
        Assert.Equal("User9", leaderboardUserDtos.First().Username); // Highest level + XP
    }
    
    [Fact]
    public async Task GetLeaderboard_Server_ReturnsTop50()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var guild = new Guild
        {
            GuildId = 1,
            GuildName = "TestGuild",
            Prefix = "!",
            Leaderboard = true,
            Media = false,
            Tiktok = false,
            MemberCount = 100
        };
        context.Guilds.Add(guild);

        for (int i = 0; i < 100; i++)
        {
            var user = new User
            {
                UserId = i,
                Username = $"User{i}",
                Discriminator = "0001"
            };

            var member = new GuildMember
            {
                GuildId = 1,
                UserId = i,
                Level = i % 10,
                Xp = 1000 - i,
                AvatarUrl = $"avatar{i}"
            };

            context.Users.Add(user);
            context.GuildMembers.Add(member);
        }

        await context.SaveChangesAsync();

        var logger = new Mock<ILogger<InformationController>>();
        var controller = new InformationController(logger.Object, context);

        var result = await controller.GetLeaderboard("1");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var leaderboard = Assert.IsAssignableFrom<IEnumerable<LeaderboardUserDto>>(ok.Value);

        var leaderboardUserDtos = leaderboard as LeaderboardUserDto[] ?? leaderboard.ToArray();
        Assert.Equal(50, leaderboardUserDtos.Count());
        Assert.Equal("User9", leaderboardUserDtos.First().Username); // Highest level + XP
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
