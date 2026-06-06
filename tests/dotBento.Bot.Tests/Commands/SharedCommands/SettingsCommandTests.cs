using dotBento.Bot.Commands.SharedCommands;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Tests.Commands.SharedCommands;

public sealed class SettingsCommandTests
{
    private static (SettingsCommand Command, TestDbFactory Factory) CreateCommand()
    {
        var factory = new TestDbFactory();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var command = new SettingsCommand(
            new GuildSettingService(factory, memoryCache),
            new UserSettingService(factory, distributedCache));
        return (command, factory);
    }

    [Fact]
    public async Task GetServerSettingsAsync_CreatesDefaultPrivateSetting()
    {
        var (command, factory) = CreateCommand();

        var response = await command.GetServerSettingsAsync(100, "Guild", "https://example.com/icon.png");
        var embed = response.Embed.Build();

        Assert.Equal("Server Settings for Guild", embed.Title);
        Assert.Equal("https://example.com/icon.png", embed.Thumbnail!.Value.Url);
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.False(db.GuildSettings.Single().LeaderboardPublic);
    }

    [Fact]
    public async Task ToggleLeaderboardPublicAsync_TogglesSetting()
    {
        var (command, factory) = CreateCommand();

        await command.ToggleLeaderboardPublicAsync(100, "Guild", null);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.True(db.GuildSettings.Single().LeaderboardPublic);
    }

    [Fact]
    public async Task GetUserSettingsAsync_CreatesDefaultUserSetting()
    {
        var (command, factory) = CreateCommand();

        var response = await command.GetUserSettingsAsync(10);

        Assert.Equal("Your Settings", response.Embed.Build().Title);
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var setting = db.UserSettings.Single();
        Assert.False(setting.HideSlashCommandCalls);
        Assert.True(setting.ShowOnGlobalLeaderboard);
    }

    [Fact]
    public async Task ToggleUserSettingsAsync_TogglesBothUserSettings()
    {
        var (command, factory) = CreateCommand();

        await command.ToggleHideCommandsAsync(10);
        await command.ToggleGlobalLeaderboardAsync(10);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var setting = db.UserSettings.Single();
        Assert.True(setting.HideSlashCommandCalls);
        Assert.False(setting.ShowOnGlobalLeaderboard);
    }
}
