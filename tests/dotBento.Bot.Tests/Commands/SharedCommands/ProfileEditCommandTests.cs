using dotBento.Bot.Commands.SharedCommands;
using dotBento.Domain.Enums;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Tests.Commands.SharedCommands;

public sealed class ProfileEditCommandTests
{
    private static (ProfileEditCommand Command, TestDbFactory Factory) CreateCommand()
    {
        var factory = new TestDbFactory();
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var command = new ProfileEditCommand(new ProfileService(cache, factory));
        return (command, factory);
    }

    [Fact]
    public async Task SetBackgroundUrlAsync_RejectsInvalidUrl()
    {
        var (command, _) = CreateCommand();

        var response = await command.SetBackgroundUrlAsync(10, "not-a-url");

        Assert.Equal(CommandResponse.Error, response.CommandResponse);
        Assert.Equal("Invalid URL", response.Embed.Build().Title);
    }

    [Fact]
    public async Task SetBackgroundColourAsync_StoresColourAndOpacity()
    {
        var (command, factory) = CreateCommand();

        var response = await command.SetBackgroundColourAsync(10, "1f2937", 80);

        Assert.Equal(CommandResponse.Ok, response.CommandResponse);
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var profile = db.Profiles.Single();
        Assert.Equal("#1F2937", profile.BackgroundColour);
        Assert.Equal(80, profile.BackgroundColourOpacity);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetDescriptionAsync_RejectsBlankDescription(string text)
    {
        var (command, _) = CreateCommand();

        var response = await command.SetDescriptionAsync(10, text);

        Assert.Equal(CommandResponse.Error, response.CommandResponse);
        Assert.Equal("Invalid description", response.Embed.Build().Title);
    }

    [Fact]
    public async Task SetAndResetProfileFields_UpdateProfile()
    {
        var (command, factory) = CreateCommand();

        await command.SetBackgroundUrlAsync(10, "https://example.com/bg.png");
        await command.SetLastFmBoardAsync(10, false);
        await command.SetXpBoardAsync(10, true);
        await command.SetDescriptionAsync(10, "hello");
        await command.SetTimezoneAsync(10, "UTC");
        await command.SetBirthdayAsync(10, "06-06");
        await command.ResetBackgroundAsync(10);
        await command.ResetDescriptionAsync(10);
        await command.ResetTimezoneAsync(10);
        await command.ResetBirthdayAsync(10);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var profile = db.Profiles.Single();
        Assert.False(profile.LastfmBoard);
        Assert.True(profile.XpBoard);
        Assert.Null(profile.BackgroundUrl);
        Assert.Null(profile.Description);
        Assert.Null(profile.Timezone);
        Assert.Null(profile.Birthday);
    }
}
