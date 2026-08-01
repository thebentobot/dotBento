using Discord;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.EntityFramework.Entities;
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
    public async Task GetServerSettingsAsync_IncludesLeaderboardAndCommandPermissionControls()
    {
        var (command, _) = CreateCommand();

        var response = await command.GetServerSettingsAsync(100, "Guild", null);
        var buttons = Buttons(response.Components);

        Assert.Collection(
            buttons,
            button => Assert.Equal("server-settings:leaderboard-public", button.CustomId),
            button =>
            {
                Assert.Equal("Command Permissions", button.Label);
                Assert.Equal("server-settings:commands:overview", button.CustomId);
                Assert.Equal(ButtonStyle.Primary, button.Style);
            });
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

    [Fact]
    public void GetCommandActionCandidates_Disable_FiltersRegisteredCommands()
    {
        var permissions = new CommandPermissions(
            ["game:rps", "removed:legacy", "server:settings"],
            ["tag:create"]);
        string[] registered = ["server:settings", "avatar", "game:rps", "tag:create", "AVATAR"];

        var candidates = SettingsCommand.GetCommandActionCandidates(
            CommandPermissionAction.Disable, permissions, registered);

        Assert.Equal(["avatar", "tag:create"], candidates);
    }

    [Fact]
    public void GetCommandActionCandidates_Enable_ContainsStoredDisabledIdsIncludingStale()
    {
        var permissions = new CommandPermissions(
            ["removed:legacy", "server:settings", "game:rps"],
            ["tag:create"]);
        string[] registered = ["server:settings", "avatar", "game:rps", "tag:create"];

        var candidates = SettingsCommand.GetCommandActionCandidates(
            CommandPermissionAction.Enable, permissions, registered);

        Assert.Equal(["game:rps", "removed:legacy", "server:settings"], candidates);
    }

    [Fact]
    public void GetCommandActionCandidates_AddAdminOnly_FiltersRegisteredCommands()
    {
        var permissions = new CommandPermissions(
            ["game:rps"],
            ["tag:create", "removed:legacy", "server:settings"]);
        string[] registered = ["server:settings", "avatar", "game:rps", "tag:create", "AVATAR"];

        var candidates = SettingsCommand.GetCommandActionCandidates(
            CommandPermissionAction.AddAdminOnly, permissions, registered);

        Assert.Equal(["avatar", "game:rps"], candidates);
    }

    [Fact]
    public void GetCommandActionCandidates_RemoveAdminOnly_ContainsStoredAdminIdsIncludingStale()
    {
        var permissions = new CommandPermissions(
            ["game:rps"],
            ["removed:legacy", "server:settings", "tag:create"]);
        string[] registered = ["server:settings", "avatar", "game:rps", "tag:create"];

        var candidates = SettingsCommand.GetCommandActionCandidates(
            CommandPermissionAction.RemoveAdminOnly, permissions, registered);

        Assert.Equal(["removed:legacy", "server:settings", "tag:create"], candidates);
    }

    [Fact]
    public async Task GetCommandActionViewAsync_WithFiftyOneCandidates_PaginatesTwentyFiveTwentyFiveOne()
    {
        var (command, _) = CreateCommand();
        var registered = Enumerable.Range(1, 51)
            .Select(index => $"test:command-{index:000}")
            .ToArray();

        var first = await command.GetCommandActionViewAsync(
            100, "Guild", null, CommandPermissionAction.Disable, 0, registered);
        var second = await command.GetCommandActionViewAsync(
            100, "Guild", null, CommandPermissionAction.Disable, 1, registered);
        var third = await command.GetCommandActionViewAsync(
            100, "Guild", null, CommandPermissionAction.Disable, 2, registered);

        var firstMenu = SelectMenu(first.Components);
        var secondMenu = SelectMenu(second.Components);
        var thirdMenu = SelectMenu(third.Components);

        Assert.Equal(25, firstMenu.Options.Count);
        Assert.Equal(25, secondMenu.Options.Count);
        Assert.Single(thirdMenu.Options);
        Assert.Equal(registered, firstMenu.Options
            .Concat(secondMenu.Options)
            .Concat(thirdMenu.Options)
            .Select(option => option.Value));
        Assert.Equal("server-settings:commands:select:disable:0", firstMenu.CustomId);
        Assert.Equal("server-settings:commands:select:disable:1", secondMenu.CustomId);
        Assert.Equal("server-settings:commands:select:disable:2", thirdMenu.CustomId);

        var firstButtons = Buttons(first.Components);
        var secondButtons = Buttons(second.Components);
        var thirdButtons = Buttons(third.Components);
        Assert.True(firstButtons.Single(button => button.Label == "Previous").IsDisabled);
        Assert.False(firstButtons.Single(button => button.Label == "Next").IsDisabled);
        Assert.False(secondButtons.Single(button => button.Label == "Previous").IsDisabled);
        Assert.False(secondButtons.Single(button => button.Label == "Next").IsDisabled);
        Assert.False(thirdButtons.Single(button => button.Label == "Previous").IsDisabled);
        Assert.True(thirdButtons.Single(button => button.Label == "Next").IsDisabled);
    }

    [Fact]
    public async Task GetCommandActionViewAsync_ExcessivePage_ClampsToLastPage()
    {
        var (command, _) = CreateCommand();
        var registered = Enumerable.Range(1, 26)
            .Select(index => $"test:command-{index:000}")
            .ToArray();

        var response = await command.GetCommandActionViewAsync(
            100, "Guild", null, CommandPermissionAction.Disable, 999, registered);
        var menu = SelectMenu(response.Components);

        var option = Assert.Single(menu.Options);
        Assert.Equal("test:command-026", option.Value);
        Assert.Equal("server-settings:commands:select:disable:1", menu.CustomId);
        Assert.Equal("Page 2 of 2 • 26 commands available", response.Embed.Build().Footer?.Text);
    }

    [Theory]
    [InlineData(CommandPermissionAction.Enable)]
    [InlineData(CommandPermissionAction.RemoveAdminOnly)]
    public async Task GetCommandActionViewAsync_NoCandidates_ShowsSafeEmptyState(
        CommandPermissionAction action)
    {
        var (command, _) = CreateCommand();

        var response = await command.GetCommandActionViewAsync(
            100, "Guild", null, action, 0, ["avatar"]);
        var embed = response.Embed.Build();

        Assert.Empty(response.Components!.ActionRows
            .SelectMany(row => row.Components)
            .OfType<SelectMenuBuilder>());
        Assert.Contains(embed.Fields, field => field.Name == "Nothing to change");
        var button = Assert.Single(Buttons(response.Components));
        Assert.Equal("server-settings:commands:overview", button.CustomId);
    }

    [Fact]
    public async Task GetCommandSettingsViewAsync_LargeOverrides_StayWithinDiscordEmbedBounds()
    {
        var (command, factory) = CreateCommand();
        var disabled = LongCommandIds("disabled", 160);
        var adminOnly = LongCommandIds("admin", 160);
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.GuildSettings.Add(new GuildSetting
            {
                GuildId = 100,
                DisabledCommands = disabled,
                AdminOnlyCommands = adminOnly
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await command.GetCommandSettingsViewAsync(
            100, "Guild", null, disabled.Concat(adminOnly).ToArray());
        var embed = response.Embed.Build();
        var textLength =
            (embed.Title?.Length ?? 0) +
            (embed.Description?.Length ?? 0) +
            (embed.Footer?.Text.Length ?? 0) +
            embed.Fields.Sum(field => field.Name.Length + field.Value.Length);

        Assert.True(embed.Fields.Length <= 25);
        Assert.All(embed.Fields, field => Assert.True(field.Value.Length <= 1024));
        Assert.True(textLength <= 6000);
        Assert.All(embed.Fields, field => Assert.Contains("more", field.Value));
    }

    private static IReadOnlyList<ButtonBuilder> Buttons(ComponentBuilder? components)
    {
        Assert.NotNull(components);
        return components.ActionRows
            .SelectMany(row => row.Components)
            .OfType<ButtonBuilder>()
            .ToArray();
    }

    private static SelectMenuBuilder SelectMenu(ComponentBuilder? components)
    {
        Assert.NotNull(components);
        return Assert.Single(components.ActionRows
            .SelectMany(row => row.Components)
            .OfType<SelectMenuBuilder>());
    }

    private static string[] LongCommandIds(string prefix, int count) =>
        Enumerable.Range(1, count)
            .Select(index => $"{prefix}:{"x".PadRight(64, 'x')}:{index:000}")
            .ToArray();
}
