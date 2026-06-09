using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using dotBento.WebApi.Controllers;
using dotBento.WebApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace dotBento.WebApi.Tests.Controllers;

public class UserSettingsControllerTests
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

    private static UserSettingsController CreateController(BotDbContext context)
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var service = new UserSettingService(new SingleContextFactory(context), cache);
        return new UserSettingsController(service);
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("-1")]
    public async Task GetUserSettings_InvalidUserId_ReturnsBadRequest(string userId)
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);

        var result = await controller.GetUserSettings(userId);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid user ID", badRequest.Value);
    }

    [Fact]
    public async Task GetUserSettings_WhenMissing_CreatesDefaultSettings()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);

        var result = await controller.GetUserSettings("42");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserSettingsDto>(ok.Value);
        Assert.False(dto.HideSlashCommandCalls);
        Assert.True(dto.ShowOnGlobalLeaderboard);

        var setting = await context.UserSettings.SingleAsync(
            s => s.UserId == 42,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(setting.HideSlashCommandCalls);
        Assert.True(setting.ShowOnGlobalLeaderboard);
    }

    [Fact]
    public async Task GetUserSettings_WhenExisting_ReturnsPersistedSettings()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        context.UserSettings.Add(new UserSetting
        {
            UserId = 42,
            HideSlashCommandCalls = true,
            ShowOnGlobalLeaderboard = false
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var controller = CreateController(context);

        var result = await controller.GetUserSettings("42");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserSettingsDto>(ok.Value);
        Assert.True(dto.HideSlashCommandCalls);
        Assert.False(dto.ShowOnGlobalLeaderboard);
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("-1")]
    public async Task UpdateUserSettings_InvalidUserId_ReturnsBadRequest(string userId)
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);

        var result = await controller.UpdateUserSettings(
            userId,
            new UserSettingsUpdateRequest(true, false));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid user ID", badRequest.Value);
    }

    [Fact]
    public async Task UpdateUserSettings_WhenMissing_CreatesAndAppliesRequest()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);

        var result = await controller.UpdateUserSettings(
            "42",
            new UserSettingsUpdateRequest(true, false));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserSettingsDto>(ok.Value);
        Assert.True(dto.HideSlashCommandCalls);
        Assert.False(dto.ShowOnGlobalLeaderboard);

        var setting = await context.UserSettings.SingleAsync(
            s => s.UserId == 42,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(setting.HideSlashCommandCalls);
        Assert.False(setting.ShowOnGlobalLeaderboard);
    }

    [Fact]
    public async Task UpdateUserSettings_NullProperties_LeaveExistingValuesUnchanged()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        context.UserSettings.Add(new UserSetting
        {
            UserId = 42,
            HideSlashCommandCalls = false,
            ShowOnGlobalLeaderboard = false
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var controller = CreateController(context);

        var result = await controller.UpdateUserSettings(
            "42",
            new UserSettingsUpdateRequest(true, null));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserSettingsDto>(ok.Value);
        Assert.True(dto.HideSlashCommandCalls);
        Assert.False(dto.ShowOnGlobalLeaderboard);

        var setting = await context.UserSettings.AsNoTracking().SingleAsync(
            s => s.UserId == 42,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(setting.HideSlashCommandCalls);
        Assert.False(setting.ShowOnGlobalLeaderboard);
    }
}
