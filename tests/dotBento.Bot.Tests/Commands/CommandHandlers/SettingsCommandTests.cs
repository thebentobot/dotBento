using Discord;
using dotBento.Bot.Commands.CommandHandlers;
using dotBento.Bot.Enums;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Tests.Commands.CommandHandlers;

public sealed class SettingsCommandTests
{
    private static (SettingsCommand Command, TestDbFactory Factory) CreateCommand(dotBento.Bot.Services.BotPresentation? presentation = null)
    {
        var factory = new TestDbFactory();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var command = new SettingsCommand(
            new GuildSettingService(factory, memoryCache),
            new UserSettingService(factory, distributedCache), presentation);
        return (command, factory);
    }

    [Fact]
    public async Task LandingPageWithoutLeaderboard_OmitsWebsiteControls()
    {
        var presentation = new dotBento.Bot.Services.BotPresentation(new()
        {
            Branding = new() { WebsiteUrl = "https://example.com", LeaderboardUrl = "" }
        });
        var (command, _) = CreateCommand(presentation);
        var server = await command.GetServerSettingsAsync(100, "Guild", null);
        Assert.DoesNotContain(server.Embed.Fields, f => f.Name == "Website Leaderboard");
        Assert.Equal("server-settings:commands:overview", Assert.Single(Buttons(server.Components)).CustomId);
        var user = await command.GetUserSettingsAsync(10);
        Assert.DoesNotContain("website", Assert.Single(user.Embed.Fields, f => f.Name == "Global Leaderboard").Value.ToString());
    }

    [Fact]
    public async Task EmptyWebsite_OmitsWebsiteControlsAndKeepsCommandSettings()
    {
        var presentation = new dotBento.Bot.Services.BotPresentation(new() { Branding = new() { WebsiteUrl = "" } });
        var (command, _) = CreateCommand(presentation);
        var server = await command.GetServerSettingsAsync(100, "Guild", null);
        Assert.DoesNotContain(server.Embed.Fields, field => field.Name.Contains("Website", StringComparison.Ordinal));
        var button = Assert.Single(Buttons(server.Components));
        Assert.Equal("server-settings:commands:overview", button.CustomId);
        var user = await command.GetUserSettingsAsync(10);
        var leaderboard = Assert.Single(user.Embed.Fields, field => field.Name == "Global Leaderboard");
        Assert.DoesNotContain("website", leaderboard.Value.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("command", leaderboard.Value.ToString());
    }

    [Fact]
    public async Task RebrandedPermissionMenu_UsesCanonicalValuesAndHidesDisabledFeatures()
    {
        var factory = new TestDbFactory();
        var memory = new MemoryCache(new MemoryCacheOptions());
        var guilds = new GuildSettingService(factory, memory);
        await guilds.SetCommandDisabledAsync(100, "bento", true);
        await guilds.SetCommandDisabledAsync(100, "media:tiktok", true);
        var presentation = new dotBento.Bot.Services.BotPresentation(new()
        {
            Branding = new() { GiftCommandName = "cookie" },
            MediaServer = new() { Enabled = false }
        });
        var command = new SettingsCommand(guilds,
            new UserSettingService(factory, new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()))),
            presentation);
        var response = await command.GetCommandActionViewAsync(100, "Guild", null,
            CommandPermissionAction.Enable, 0, ["bento", "ping"]);
        var option = Assert.Single(SelectMenu(response.Components).Options);
        Assert.Equal("cookie", option.Label);
        Assert.Equal("bento", option.Value);
        var permissions = await guilds.GetCommandPermissionsAsync(100);
        Assert.Contains("media:tiktok", permissions.Disabled);
        await command.ApplyCommandPermissionActionAsync(100, option.Value, CommandPermissionAction.Enable);
        permissions = await guilds.GetCommandPermissionsAsync(100);
        Assert.DoesNotContain("bento", permissions.Disabled);
        Assert.Contains("media:tiktok", permissions.Disabled);
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

    [Fact]
    public async Task GetUserSettingsAsync_IncludesDeleteDataButton()
    {
        var factory = new TestDbFactory();
        var command = CreateCommandWithPrivacy(factory, CreatePrivacyService(factory));

        var response = await command.GetUserSettingsAsync(10);

        Assert.Contains(Buttons(response.Components),
            button => button.CustomId == "user-settings:delete-data");
    }

    [Fact]
    public void GetDeleteDataConfirmationAsync_ExplainsOptOutAndKeepsOnlyUserId()
    {
        var factory = new TestDbFactory();
        var command = CreateCommandWithPrivacy(factory, CreatePrivacyService(factory));

        var response = command.GetDeleteDataConfirmationAsync();
        var embed = response.Embed.Build();

        Assert.Equal("Delete your data", embed.Title);
        Assert.Contains("Discord user ID", embed.Description);
        Assert.Contains("kick", embed.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Collection(
            Buttons(response.Components),
            button => Assert.Equal("user-settings:delete-data:confirm", button.CustomId),
            button => Assert.Equal("user-settings:root", button.CustomId));
    }

    [Fact]
    public async Task DeleteUserDataAsync_OptsOutAndDeletesUserData()
    {
        var factory = new TestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(new User { UserId = 10, Username = "User", Discriminator = "0001", Level = 1, Xp = 0 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var command = CreateCommandWithPrivacy(factory, CreatePrivacyService(factory));

        var response = await command.DeleteUserDataAsync(10);

        Assert.Equal("Your data has been deleted", response.Embed.Build().Title);
        await using var assertDb = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.True(await assertDb.UserOptOuts.AnyAsync(o => o.UserId == 10, TestContext.Current.CancellationToken));
        Assert.False(await assertDb.Users.AnyAsync(u => u.UserId == 10, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetUserSettingsAsync_WhenOptedOut_ShowsOptInView()
    {
        var factory = new TestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.UserOptOuts.Add(new UserOptOut { UserId = 10, OptedOutAt = DateTime.UtcNow });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var command = CreateCommandWithPrivacy(factory, CreatePrivacyService(factory));

        var response = await command.GetUserSettingsAsync(10);

        Assert.Equal("You are opted out", response.Embed.Build().Title);
        var button = Assert.Single(Buttons(response.Components));
        Assert.Equal("user-settings:opt-in", button.CustomId);
    }

    [Fact]
    public async Task OptBackInAsync_RemovesOptOutWithoutCreatingSettingsForDeletedUser()
    {
        var factory = new TestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.UserOptOuts.Add(new UserOptOut { UserId = 10, OptedOutAt = DateTime.UtcNow });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var command = CreateCommandWithPrivacy(factory, CreatePrivacyService(factory));

        var response = await command.OptBackInAsync(10);

        Assert.Equal("You are opted back in", response.Embed.Build().Title);
        Assert.Contains("/user settings", response.Embed.Build().Description);
        await using var assertDb = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.False(await assertDb.UserOptOuts.AnyAsync(o => o.UserId == 10, TestContext.Current.CancellationToken));
        Assert.Empty(assertDb.UserSettings);
        Assert.Empty(assertDb.Users);
    }

    private static PrivacyService CreatePrivacyService(TestDbFactory factory) =>
        new(factory, new MemoryCache(new MemoryCacheOptions()));

    private static SettingsCommand CreateCommandWithPrivacy(TestDbFactory factory, PrivacyService privacy)
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return new SettingsCommand(
            new GuildSettingService(factory, memoryCache),
            new UserSettingService(factory, distributedCache),
            null, privacy);
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
