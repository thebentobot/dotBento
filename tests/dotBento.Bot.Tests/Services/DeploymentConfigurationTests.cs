using Discord;
using dotBento.Bot.Commands.CommandHandlers;
using dotBento.Bot.Enums;
using dotBento.Bot.Models;
using dotBento.Bot.Services;
using dotBento.CommandDocs;
using dotBento.Domain;
using dotBento.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Tests.Services;

public sealed class DeploymentConfigurationTests
{
    [Fact]
    public async Task Rebranding_ChangesInvocationsAndDescriptionsButPreservesPermissionIds()
    {
        var config = new BotEnvConfig
        {
            Branding = new()
            {
                Name = "Mochi", GiftName = "Cookie", GiftPluralName = "Cookies",
                GiftCommandName = "cookie", AboutCommandName = "mochi", GiftLeaderboardName = "cookies"
            }
        };
        var manifest = await CommandManifestBuilder.BuildAsync(config);
        var gift = Assert.Single(manifest.Commands, c => c.Id == "bento");
        Assert.Equal("/cookie", gift.Invocation);
        Assert.Equal("Give a friend a Cookie", gift.Description);
        Assert.Equal("/about mochi", Assert.Single(manifest.Commands, c => c.Id == "about:bento").Invocation);
        Assert.Equal("/leaderboard cookies server", Assert.Single(manifest.Commands, c => c.Id == "leaderboard:bento:server").Invocation);
        Assert.DoesNotContain(manifest.Commands, c => c.Description.Contains("Bento", StringComparison.Ordinal));
        Assert.Equal(["ManageGuild"], Assert.Single(manifest.Commands, c => c.Id == "server:settings").RequiredUserPermissions);
        Assert.Equal("/bento", Assert.Single((await CommandManifestBuilder.BuildAsync()).Commands, c => c.Id == "bento").Invocation);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("ping")]
    [InlineData("weather")]
    [InlineData("media")]
    [InlineData("horoscope")]
    public async Task CommandCollisions_FailBeforeRegistration(string name)
    {
        var config = new BotEnvConfig { Branding = new() { GiftCommandName = name } };
        await Assert.ThrowsAnyAsync<ArgumentException>(() => CommandManifestBuilder.BuildAsync(config));
    }

    [Fact]
    public async Task InvalidDescriptions_AreRejected()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => CommandManifestBuilder.BuildAsync(
            new() { Branding = new() { Name = new string('a', 101) } }));
    }

    [Theory]
    [InlineData("rps")]
    [InlineData("global")]
    [InlineData("server")]
    [InlineData("user")]
    public async Task LeaderboardCommandAndGroupCollisions_AreRejected(string name)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => CommandManifestBuilder.BuildAsync(
            new() { Branding = new() { GiftLeaderboardName = name } }));
    }

    [Theory]
    [InlineData("#FFFFF")]
    [InlineData("#GGGGGG")]
    [InlineData("1234567")]
    [InlineData("##123456")]
    public void InvalidColours_AreRejected(string colour) =>
        Assert.Throws<ArgumentException>(() => new BotPresentation(new() { Theme = new() { Primary = colour } }));

    [Fact]
    public void InvalidUrlsNamesAndPagination_AreRejected()
    {
        Assert.Throws<ArgumentException>(() => new BotPresentation(new() { Branding = new() { WebsiteUrl = "relative/path" } }));
        Assert.Throws<ArgumentException>(() => new BotPresentation(new() { Branding = new() { GiftCommandName = "Bad Name" } }));
        Assert.Throws<ArgumentException>(() => new BotPresentation(new() { Pagination = new() { First = "invalid" } }));
        Assert.Throws<ArgumentException>(() => new BotPresentation(new() { Pagination = new() { First = "▶️" } }));
    }

    [Fact]
    public async Task MatchingPrimaryAndErrorColours_DoNotChangeResponseStatus()
    {
        var presentation = new BotPresentation(new() { Theme = new() { Primary = "FF0000", Error = "#FF0000" } });
        var success = await ChooseCommand.Command("one,two", presentation);
        var failure = await ChooseCommand.Command("one", presentation);
        Assert.Equal(new Color(0xFF0000), success.Embed.Color);
        Assert.Equal(success.Embed.Color, failure.Embed.Color);
        Assert.Equal(CommandResponse.Ok, success.CommandResponse);
        Assert.Equal(CommandResponse.Error, failure.CommandResponse);
    }

    [Fact]
    public void EmptyLinks_AreOmittedAndBrandingIsApplied()
    {
        var presentation = new BotPresentation(new()
        {
            Branding = new()
            {
                Name = "Mochi", Emoji = "🍡", Description = "Hello there",
                WebsiteUrl = "", DocumentationUrl = "", PatreonUrl = "", KoFiUrl = "", VoteUrl = "", SupportUrl = ""
            },
            Theme = new() { Primary = "123456" }
        });
        var response = AboutCommand.BuildEmbed("https://example.com/avatar.png", presentation);
        Assert.Equal("About Mochi Bot 🍡", response.Embed.Title);
        Assert.Equal("Hello there", response.Embed.Description);
        Assert.Equal(new Color(0x123456), response.Embed.Color);
        Assert.Empty(response.Embed.Fields);
        Assert.Equal("", presentation.TroubleshootingHelp);
        Assert.Equal("https://example.com/docs/troubleshooting/", Branding.JoinUrl("https://example.com/docs/", "/troubleshooting/"));
        Assert.Equal("", Branding.JoinUrl("", "leaderboard"));
    }

    [Fact]
    public void Pagination_UsesPortableDefaultsAndAcceptsCustomEmoji()
    {
        var portable = new BotPresentation();
        Assert.All(portable.PaginationEmotes.Keys, emote => Assert.IsType<Emoji>(emote));
        var custom = new BotPresentation(new() { Pagination = new() { First = "<:first:883825508633182208>" } });
        Assert.Contains(custom.PaginationEmotes.Keys, emote => emote is Emote { Id: 883825508633182208 });
    }


}
