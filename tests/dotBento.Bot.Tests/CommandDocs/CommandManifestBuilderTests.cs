using dotBento.CommandDocs;

namespace dotBento.Bot.Tests.CommandDocs;

public sealed class CommandManifestBuilderTests
{
    [Fact]
    public async Task BuildAsync_ExportsEverySlashCommandInInvocationOrder()
    {
        var manifest = await CommandManifestBuilder.BuildAsync();

        Assert.Equal(1, manifest.SchemaVersion);
        Assert.Equal(69, manifest.Commands.Count);
        Assert.Equal(
            manifest.Commands.Select(command => command.Invocation).Order(StringComparer.Ordinal),
            manifest.Commands.Select(command => command.Invocation));
        Assert.Equal(manifest.Commands.Count, manifest.Commands.Select(command => command.Id).Distinct().Count());
    }

    [Fact]
    public async Task BuildAsync_RepresentsUngroupedAndNestedCommandPaths()
    {
        var manifest = await CommandManifestBuilder.BuildAsync();

        var ungrouped = Assert.Single(manifest.Commands, command => command.Id == "ping");
        Assert.Equal(["ping"], ungrouped.Path);
        Assert.Equal("/ping", ungrouped.Invocation);
        Assert.Empty(ungrouped.GroupPath);

        var nested = Assert.Single(manifest.Commands, command => command.Id == "profile:background:colour");
        Assert.Equal(["profile", "background", "colour"], nested.Path);
        Assert.Equal("/profile background colour", nested.Invocation);
        Assert.Collection(
            nested.GroupPath,
            group =>
            {
                Assert.Equal("profile", group.Name);
                Assert.Equal("View and edit your Bento profile settings", group.Description);
            },
            group =>
            {
                Assert.Equal("background", group.Name);
                Assert.Equal("Background settings for your profile", group.Description);
            });
    }

    [Fact]
    public async Task BuildAsync_ExportsGuildOnlyAndManageGuildRequirements()
    {
        var manifest = await CommandManifestBuilder.BuildAsync();

        var serverSettings = Assert.Single(manifest.Commands, command => command.Id == "server:settings");
        Assert.True(serverSettings.GuildOnly);
        Assert.Equal(["ManageGuild"], serverSettings.RequiredUserPermissions);
        Assert.Empty(serverSettings.Options);

        var serverInfo = Assert.Single(manifest.Commands, command => command.Id == "server:info");
        Assert.True(serverInfo.GuildOnly);
        Assert.Empty(serverInfo.RequiredUserPermissions);
    }

    [Fact]
    public async Task BuildAsync_ExportsDefaultsChoicesAutocompleteAndRequiredOptions()
    {
        var manifest = await CommandManifestBuilder.BuildAsync();

        var roll = Assert.Single(manifest.Commands, command => command.Id == "game:roll");
        var minimum = Assert.Single(roll.Options, option => option.Name == "min");
        Assert.Equal("Integer", minimum.Type);
        Assert.False(minimum.Required);
        Assert.Equal(1, minimum.DefaultValue);

        var topArtists = Assert.Single(manifest.Commands, command => command.Id == "lastfm:topartists");
        var timePeriod = Assert.Single(topArtists.Options, option => option.Name == "time-period");
        Assert.Equal("Overall", timePeriod.DefaultValue);
        Assert.Equal(
            ["Overall", "7 Days", "1 Month", "3 Months", "6 Months", "1 Year"],
            timePeriod.Choices.Select(choice => choice.Name));
        Assert.Equal(timePeriod.Choices.Select(choice => choice.Name), timePeriod.Choices.Select(choice => choice.Value));

        var timezone = Assert.Single(manifest.Commands, command => command.Id == "profile:timezone");
        var timezoneId = Assert.Single(timezone.Options);
        Assert.Equal("id", timezoneId.Name);
        Assert.Equal("String", timezoneId.Type);
        Assert.True(timezoneId.Required);
        Assert.True(timezoneId.Autocomplete);
        Assert.Equal("Timezone ID, e.g. Europe/Oslo or Pacific Standard Time", timezoneId.Description);

        Assert.DoesNotContain(
            manifest.Commands.SelectMany(command => command.Options),
            option => option.MinValue is -9007199254740991 || option.MaxValue is 9007199254740991);
    }

    [Fact]
    public async Task BuildAsync_SerializesDeterministically()
    {
        var first = ManifestJson.Serialize(await CommandManifestBuilder.BuildAsync());
        var second = ManifestJson.Serialize(await CommandManifestBuilder.BuildAsync());

        Assert.Equal(first, second);
        Assert.EndsWith("\n", first);
        Assert.DoesNotContain("\r\n", first, StringComparison.Ordinal);
    }
}
