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
