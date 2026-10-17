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
    public void Pagination_UsesPortableDefaultsAndAcceptsCustomEmoji()
    {
        var portable = new BotPresentation();
        Assert.All(portable.PaginationEmotes.Keys, emote => Assert.IsType<Emoji>(emote));
        var custom = new BotPresentation(new() { Pagination = new() { First = "<:first:883825508633182208>" } });
        Assert.Contains(custom.PaginationEmotes.Keys, emote => emote is Emote { Id: 883825508633182208 });
    }


}
