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
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void RewardSwitches_ChangePromisesIndependentlyAndKeepLinks(bool donation, bool voting)
    {
        var presentation = new BotPresentation(new()
        {
            Features = new() { DonationRewardsEnabled = donation, VotingRewardsEnabled = voting }
        });
        foreach (var response in new[]
        {
            AboutCommand.BuildEmbed("https://example.com/avatar.png", presentation),
            dotBento.Bot.Handlers.ClientJoinedGuildHandler.BuildWelcomeEmbed("https://example.com/avatar.png", presentation)
        })
        {
            var tip = Assert.Single(response.Embed.Fields, f => f.Value.ToString() == presentation.Brand.KoFiUrl);
            var vote = Assert.Single(response.Embed.Fields, f => f.Value.ToString() == presentation.Brand.VoteUrl);
            Assert.Equal(donation, tip.Name.Contains("for each tip", StringComparison.Ordinal));
            Assert.Equal(voting, vote.Name.Contains("receive 5", StringComparison.Ordinal));
            Assert.Equal(donation ? "Get a Bento Box 🍱 for each tip" : "Support Bento on Ko-fi", tip.Name);
            Assert.Equal(voting ? "Vote on top.gg and receive 5 Bento Boxes 🍱" : "Vote on top.gg", vote.Name);
        }
        Assert.True(new FeatureConfig().DonationRewardsEnabled);
        Assert.True(new FeatureConfig().VotingRewardsEnabled);
    }

    [Fact]
    public void WebsitePages_CanBeDisabledIndependentlyOfLandingPage()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Branding:WebsiteUrl"] = "https://example.com",
            ["Branding:FeaturesUrl"] = "",
            ["Branding:LeaderboardUrl"] = "",
            ["Branding:TroubleshootingUrl"] = "",
            ["Branding:ServerSettingsGuideUrl"] = ""
        }).Build().Get<BotEnvConfig>()!;
        var presentation = new BotPresentation(config);
        Assert.True(presentation.HasWebsite);
        Assert.False(presentation.HasWebsiteLeaderboard);
        Assert.Empty(presentation.TroubleshootingHelp);
        Assert.Empty(config.Branding.GetLeaderboardUrl(123));
        var about = AboutCommand.BuildEmbed("https://example.com/avatar.png", presentation);
        Assert.DoesNotContain(about.Embed.Fields, f => f.Name.Contains("features", StringComparison.Ordinal));
        var welcome = dotBento.Bot.Handlers.ClientJoinedGuildHandler.BuildWelcomeEmbed("https://example.com/avatar.png", presentation);
        Assert.Contains(welcome.Embed.Fields, f => f.Value.ToString() == "https://example.com");
        var settings = Assert.Single(welcome.Embed.Fields, f => f.Name.StartsWith("Configure", StringComparison.Ordinal));
        Assert.DoesNotContain("website", settings.Value.ToString());
        Assert.DoesNotContain("server settings guide", settings.Value.ToString());
    }

    [Fact]
    public void WebsitePages_InheritExistingRoutesOrUseIndependentOverrides()
    {
        var defaults = new Branding();
        Assert.Equal("https://bentobot.xyz/features", defaults.GetFeaturesUrl());
        Assert.Equal("https://bentobot.xyz/leaderboard/123", defaults.GetLeaderboardUrl(123));
        Assert.Equal("https://docs.bentobot.xyz/troubleshooting/", defaults.GetTroubleshootingUrl());
        Assert.Equal("https://docs.bentobot.xyz/guides/server-settings/", defaults.GetServerSettingsGuideUrl());
        var brand = new Branding
        {
            WebsiteUrl = "", DocumentationUrl = "", FeaturesUrl = "https://pages.example/about",
            LeaderboardUrl = "https://stats.example/rankings/", TroubleshootingUrl = "https://help.example/issues",
            ServerSettingsGuideUrl = "https://help.example/setup"
        };
        var presentation = new BotPresentation(new() { Branding = brand });
        Assert.True(presentation.HasWebsiteLeaderboard);
        Assert.Equal("https://stats.example/rankings/123", brand.GetLeaderboardUrl(123));
        Assert.Contains("https://help.example/issues", presentation.TroubleshootingHelp);
        Assert.Equal("https://help.example/setup", brand.GetServerSettingsGuideUrl());
        Assert.Contains(AboutCommand.BuildEmbed("https://example.com/avatar.png", presentation).Embed.Fields,
            f => f.Value.ToString() == "https://pages.example/about");
        Assert.Throws<ArgumentException>(() => new BotPresentation(new() { Branding = new() { FeaturesUrl = "relative" } }));
    }

    [Fact]
    public void EnvironmentOverrides_BindWithoutChangingOtherHosts()
    {
        const string prefix = "DOTBENTO_TEST_";
        Environment.SetEnvironmentVariable(prefix + "Branding__Name", "Mochi");
        Environment.SetEnvironmentVariable(prefix + "MediaServer__Enabled", "false");
        try
        {
            var config = new ConfigurationBuilder().AddEnvironmentVariables(prefix).Build().Get<BotEnvConfig>()!;
            Assert.Equal("Mochi", new BotPresentation(config).Brand.Name);
            Assert.False(config.MediaEnabled);
            Assert.False(config.HoroscopeEnabled);
            Assert.Equal("Bento", new BotPresentation().Brand.Name);
            Assert.True(new BotEnvConfig().MediaEnabled);
        }
        finally
        {
            Environment.SetEnvironmentVariable(prefix + "Branding__Name", null);
            Environment.SetEnvironmentVariable(prefix + "MediaServer__Enabled", null);
        }
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task FeatureSwitches_FilterCommandsAndPreserveUnrelatedCommands(bool master, bool media, bool horoscope)
    {
        var config = new BotEnvConfig
        {
            MediaServer = new() { Enabled = master },
            Features = new() { MediaEnabled = media, HoroscopeEnabled = horoscope }
        };
        var manifest = await CommandManifestBuilder.BuildAsync(config);
        Assert.Equal(master && media ? 2 : 0, manifest.Commands.Count(c => c.Path[0] == "media"));
        Assert.Equal(master && horoscope ? 6 : 0, manifest.Commands.Count(c => c.Path[0] == "horoscope"));
        Assert.Contains(manifest.Commands, c => c.Id == "ping");
        Assert.Contains(manifest.Commands, c => c.Id == "server:settings");
        Assert.Equal(!(master && media), CommandModules.IsDisabled("media", config));
        Assert.Equal(!(master && horoscope), CommandModules.IsDisabled("horoscope-settings:remove:123", config));
        Assert.False(CommandModules.IsDisabled("server-settings:root", config));
    }

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
        var config = new BotEnvConfig { Branding = new() { GiftCommandName = name }, MediaServer = new() { Enabled = false } };
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
    public void DisabledPatreon_OmitsPromotionEvenWhenUrlIsConfigured()
    {
        var disabled = new BotPresentation(new() { Features = new() { PatreonEnabled = false } });
        var hidden = AboutCommand.BuildEmbed("https://example.com/avatar.png", disabled);
        Assert.DoesNotContain(hidden.Embed.Fields, field => field.Value.ToString() == disabled.Brand.PatreonUrl);
        var enabled = new BotPresentation();
        var shown = AboutCommand.BuildEmbed("https://example.com/avatar.png", enabled);
        Assert.Contains(shown.Embed.Fields, field => field.Value.ToString() == enabled.Brand.PatreonUrl);
        Assert.True(new BotEnvConfig().Features.PatreonEnabled);
    }

    [Fact]
    public void WebsiteConfiguration_DistinguishesOmittedAndExplicitlyEmptyValues()
    {
        var omitted = new ConfigurationBuilder().Build().Get<BotEnvConfig>() ?? new();
        Assert.Equal("https://bentobot.xyz", omitted.Branding.WebsiteUrl);
        var empty = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Branding:WebsiteUrl"] = "" }).Build().Get<BotEnvConfig>()!;
        Assert.Equal("", empty.Branding.WebsiteUrl);
        var presentation = new BotPresentation(empty);
        var welcome = dotBento.Bot.Handlers.ClientJoinedGuildHandler.BuildWelcomeEmbed("https://example.com/avatar.png", presentation);
        Assert.Null(welcome.Embed.Author.Url);
        Assert.DoesNotContain(welcome.Embed.Fields, field => field.Name.Contains("website", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(welcome.Embed.Fields, field => field.Value.ToString()!.Contains("website", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DisabledPatreon_OmitsWelcomePromotion()
    {
        var disabled = new BotPresentation(new() { Features = new() { PatreonEnabled = false } });
        var welcome = dotBento.Bot.Handlers.ClientJoinedGuildHandler.BuildWelcomeEmbed("https://example.com/avatar.png", disabled);
        Assert.DoesNotContain(welcome.Embed.Fields, field => field.Value.ToString() == disabled.Brand.PatreonUrl);
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

    [Theory]
    [InlineData("local", true)]
    [InlineData("staging", true)]
    [InlineData("production", false)]
    [InlineData("PRODUCTION", false)]
    public void GuildRetention_HasEnvironmentDefaultsAndExplicitOverrides(string environment, bool expected)
    {
        var config = new BotEnvConfig { Environment = environment };
        Assert.Equal(expected, config.KeepGuildDataOnLeave);
        config.Bot.KeepGuildDataOnLeave = !expected;
        Assert.Equal(!expected, config.KeepGuildDataOnLeave);
    }

    [Fact]
    public async Task DisabledApiFeatures_ReturnBeforeHttpOrDatabaseAccess()
    {
        var config = Options.Create(new BotEnvConfig { MediaServer = new() { Enabled = false } });
        // Null collaborators deliberately fail if the disabled path attempts any access.
        var media = new MediaCommand(null!, config);
        var horoscope = new HoroscopeCommand(null!, null!, config);
        var responses = new[]
        {
            await media.GetMediaResponseAsync("https://example.com/post"),
            await horoscope.GetHoroscopeAsync(1, "today"),
            await horoscope.SaveHoroscopeAsync(1, "aries"),
            await horoscope.RemoveHoroscopeAsync(1),
            await horoscope.GetSettingsAsync(1)
        };
        Assert.All(responses, response =>
        {
            Assert.Equal(ResponseType.Embed, response.ResponseType);
            Assert.Equal(CommandResponse.Error, response.CommandResponse);
            Assert.Contains("disabled", response.Embed.Title, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Activity_UsesConfiguredTextAndFallsBackToLiveCounts()
    {
        using var client = new Discord.WebSocket.DiscordSocketClient();
        var custom = dotBento.Bot.Services.BackgroundService.CreateActivity(client, new() { Status = "Eating cookies" });
        Assert.Equal("Eating cookies", custom.Name);
        Assert.Equal(ActivityType.Watching, custom.Type);
        var automatic = dotBento.Bot.Services.BackgroundService.CreateActivity(client, new());
        Assert.Equal("0 users on 0 servers", automatic.Name);
    }

    [Fact]
    public async Task HoroscopePreferences_SurviveDisablingAndReenabling()
    {
        var factory = new TestDbFactory();
        var service = new dotBento.Infrastructure.Services.HoroscopeService(factory);
        await service.SaveHoroscopeAsync(1, "libra");
        var disabled = new HoroscopeCommand(service, null!, Options.Create(new BotEnvConfig
        {
            Features = new() { HoroscopeEnabled = false }
        }));
        Assert.Equal(CommandResponse.Error, (await disabled.SaveHoroscopeAsync(1, "aries")).CommandResponse);
        Assert.Equal(CommandResponse.Error, (await disabled.RemoveHoroscopeAsync(1)).CommandResponse);
        var enabled = new HoroscopeCommand(service, null!, Options.Create(new BotEnvConfig()));
        var settings = await enabled.GetSettingsAsync(1);
        Assert.Contains("Libra", settings.Embed.Description);
        var saved = await service.GetHoroscopeAsync(1);
        Assert.True(saved.HasValue);
        Assert.Equal("libra", saved.Value.Sign);
    }

    [Fact]
    public async Task EnabledWithoutApiConfiguration_ReturnsUnavailableResponse()
    {
        var config = Options.Create(new BotEnvConfig());
        var media = await new MediaCommand(null!, config).GetMediaResponseAsync("https://example.com/post");
        var horoscope = await new HoroscopeCommand(null!, null!, config).GetHoroscopeAsync(1, "today", "aries");
        Assert.Equal("Media commands are not enabled", media.Embed.Title);
        Assert.Equal("Horoscope commands are not enabled", horoscope.Embed.Title);
    }
}
