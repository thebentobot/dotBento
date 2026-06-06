using System.Reflection;
using System.Net;
using CSharpFunctionalExtensions;
using dotBento.Domain.Entities;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services;
using dotBento.Infrastructure.Services.Api;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using DomainProfile = dotBento.Domain.Entities.Profile;
using EfGuild = dotBento.EntityFramework.Entities.Guild;
using EfGuildMember = dotBento.EntityFramework.Entities.GuildMember;
using EfUser = dotBento.EntityFramework.Entities.User;

namespace dotBento.Infrastructure.Tests;

public class ProfileCommandsTests
{
    // Helper to invoke private static methods via reflection
    private static T? InvokePrivateStatic<T>(Type type, string methodName, params object?[]? args)
    {
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var result = method.Invoke(null, args);
        return result is null ? default : (T)result;
    }

    private static async Task<T> InvokePrivateInstanceAsync<T>(object instance, string methodName, params object?[]? args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var task = (Task<T>)method.Invoke(instance, args)!;
        return await task;
    }

    [Theory]
    [InlineData("02-07", "Feb 7 🎂")]           // MM-dd
    [InlineData("2-7", "Feb 7 🎂")]             // M-d
    [InlineData("02/07", "Feb 7 🎂")]          // MM/dd
    [InlineData("2/7", "Feb 7 🎂")]            // M/d
    [InlineData("07-02", "Jul 2 🎂")]          // MM-dd (new syntax)
    [InlineData("7-2", "Jul 2 🎂")]            // M-d (new syntax)
    [InlineData("07/02", "Jul 2 🎂")]          // MM/dd (new syntax)
    [InlineData("7/2", "Jul 2 🎂")]            // M/d (new syntax)
    [InlineData("7 february", "Feb 7 🎂")]     // text, lowercase month
    [InlineData("February 18", "Feb 18 🎂")]   // text, Month d
    [InlineData("20 April 2000", "Apr 20 🎂")] // text with year
    [InlineData("25 Nov", "Nov 25 🎂")]        // short month
    [InlineData("  February   1  ", "Feb 1 🎂")] // extra spaces
    public void FormatBirthday_ValidInputs_ReturnsNormalized(string input, string expected)
    {
        var result = InvokePrivateStatic<string>(typeof(ProfileCommands), "FormatBirthday", input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a date")] // invalid
    public void FormatBirthday_InvalidOrEmpty_ReturnsEmpty(string? input)
    {
        var result = InvokePrivateStatic<string>(typeof(ProfileCommands), "FormatBirthday", input);
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData(0, "🌌")]
    [InlineData(3, "🌌")]
    [InlineData(4, "🌅")]
    [InlineData(7, "🌅")]
    [InlineData(8, "☀️")]
    [InlineData(11, "☀️")]
    [InlineData(12, "🌞")]
    [InlineData(15, "🌞")]
    [InlineData(16, "🌇")]
    [InlineData(19, "🌇")]
    [InlineData(20, "🌙")]
    [InlineData(23, "🌙")]
    public void ShowEmoteAccordingToTimeOfDay_MapsHoursToEmotes(int hour, string expected)
    {
        var dt = new DateTime(2024, 1, 1, hour, 0, 0, DateTimeKind.Unspecified);
        var result = InvokePrivateStatic<string>(typeof(ProfileCommands), "ShowEmoteAccordingToTimeOfDay", dt);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(100, "FF")]
    [InlineData(0, "00")]
    [InlineData(50, "7F")] // 127.5 -> 127 -> 0x7F
    [InlineData(null, "FF")]
    public void ConvertOpacityToHex_WorksAsExpected(int? percent, string expectedHex)
    {
        var result = InvokePrivateStatic<string>(typeof(ProfileCommands), "ConvertOpacityToHex", percent);
        Assert.Equal(expectedHex, result);
    }

    [Theory]
    [InlineData("ShortName", "24px")]           // <= 15
    [InlineData("ThisIsEighteenLong", "18px")] // length 18 => <= 20
    [InlineData("abcdefghijklmnopqrstuv", "15px")] // length 22 => <= 25
    [InlineData("ThisUserNameIsWayTooLongForLargeFont", "11px")] // > 25
    public void UsernamePxSize_ScalesByLength(string username, string expectedPx)
    {
        var result = InvokePrivateStatic<string>(typeof(ProfileCommands), "UsernamePxSize", username);
        Assert.Equal(expectedPx, result);
    }

    [Theory]
    [InlineData(10, "16px")]
    [InlineData(38, "14px")]
    [InlineData(44, "12px")]
    [InlineData(60, "10px")]
    public void LastFmTextPxSize_ScalesByLength(int length, string expectedPx)
    {
        var result = InvokePrivateStatic<string>(typeof(ProfileCommands), "LastFmTextPxSize", length);
        Assert.Equal(expectedPx, result);
    }

    [Fact]
    public void MergeWithDefaults_FillsMissingProfileSettings()
    {
        var partial = new DomainProfile(
            UserId: 10,
            LastfmBoard: null,
            XpBoard: null,
            BackgroundUrl: null,
            BackgroundColourOpacity: null,
            BackgroundColour: null,
            DescriptionColourOpacity: null,
            DescriptionColour: null,
            OverlayOpacity: null,
            OverlayColour: null,
            UsernameColour: null,
            DiscriminatorColour: null,
            SidebarItemServerColour: null,
            SidebarItemGlobalColour: null,
            SidebarItemBentoColour: null,
            SidebarItemTimezoneColour: null,
            SidebarValueServerColour: null,
            SidebarValueGlobalColour: null,
            SidebarValueBentoColour: null,
            SidebarOpacity: null,
            SidebarColour: null,
            SidebarBlur: null,
            FmDivBgOpacity: null,
            FmDivBgColour: null,
            FmSongTextOpacity: null,
            FmSongTextColour: null,
            FmArtistTextOpacity: null,
            FmArtistTextColour: null,
            XpDivBgOpacity: null,
            XpDivBgColour: null,
            XpTextOpacity: null,
            XpTextColour: null,
            XpText2Opacity: null,
            XpText2Colour: null,
            XpDoneServerColour1Opacity: null,
            XpDoneServerColour1: null,
            XpDoneServerColour2Opacity: null,
            XpDoneServerColour2: null,
            XpDoneServerColour3Opacity: null,
            XpDoneServerColour3: null,
            XpDoneGlobalColour1Opacity: null,
            XpDoneGlobalColour1: null,
            XpDoneGlobalColour2Opacity: null,
            XpDoneGlobalColour2: null,
            XpDoneGlobalColour3Opacity: null,
            XpDoneGlobalColour3: null,
            Description: "keep me",
            Timezone: null,
            Birthday: null,
            XpBarOpacity: null,
            XpBarColour: null,
            XpBar2Opacity: null,
            XpBar2Colour: null);

        var result = InvokePrivateStatic<DomainProfile>(typeof(ProfileCommands), "MergeWithDefaults", partial)!;

        Assert.False(result.LastfmBoard);
        Assert.True(result.XpBoard);
        Assert.Equal("#1F2937", result.BackgroundColour);
        Assert.Equal(20, result.OverlayOpacity);
        Assert.Equal("#ffffff", result.UsernameColour);
        Assert.Equal("#111827", result.FmDivBgColour);
        Assert.Equal("#374151", result.XpBarColour);
        Assert.Equal("keep me", result.Description);
    }

    [Fact]
    public async Task GetUserXpBoardHtml_ReturnsBoardWhenUserGuildAndMemberExist()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(new EfUser { UserId = 10, Username = "User", Discriminator = "0001", Level = 3, Xp = 50 });
            db.Guilds.Add(new EfGuild
            {
                GuildId = 100,
                GuildName = "Guild",
                Prefix = "!",
                Icon = "guild.png",
                Leaderboard = true,
                Media = false,
                Tiktok = false
            });
            db.GuildMembers.Add(new EfGuildMember { GuildId = 100, UserId = 10, Level = 2, Xp = 25 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var command = CreateCommand(factory);
        var profile = DefaultProfile(10);

        var result = await InvokePrivateInstanceAsync<Maybe<string>>(
            command,
            "GetUserXpBoardHtml",
            profile,
            100L,
            "bot.png");

        Assert.True(result.HasValue);
        Assert.Contains("guild.png", result.Value);
        Assert.Contains("Level 2", result.Value);
        Assert.Contains("bot.png", result.Value);
        Assert.Contains("Level 3", result.Value);
    }

    [Fact]
    public async Task GetUserEmotes_AddsSupportDeveloperAndPatreonEmotes()
    {
        const long developerUserId = 232584569289703424;
        const long supportGuildId = 714496317522444352;
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(new EfUser { UserId = developerUserId, Username = "Dev", Discriminator = "0001", Level = 1, Xp = 0 });
            db.Guilds.Add(new EfGuild
            {
                GuildId = supportGuildId,
                GuildName = "Support",
                Prefix = "!",
                Leaderboard = true,
                Media = false,
                Tiktok = false
            });
            db.GuildMembers.Add(new EfGuildMember { GuildId = supportGuildId, UserId = developerUserId, Level = 1, Xp = 0 });
            db.Patreons.Add(new Patreon
            {
                UserId = developerUserId,
                Name = "Patron",
                Avatar = "avatar.png",
                Sponsor = true,
                EmoteSlot1 = "one.png",
                EmoteSlot2 = "two.png",
                EmoteSlot3 = "three.png",
                EmoteSlot4 = "four.png"
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var command = CreateCommand(factory);

        var emotes = await InvokePrivateInstanceAsync<string[]>(command, "GetUserEmotes", developerUserId);

        Assert.Contains("🍱", emotes);
        Assert.Contains("👨‍💻", emotes);
        Assert.Contains("""<img src="one.png" width="24" height="24">""", emotes);
        Assert.Contains("""<img src="two.png" width="24" height="24">""", emotes);
        Assert.Contains("""<img src="three.png" width="24" height="24">""", emotes);
        Assert.Contains("""<img src="four.png" width="24" height="24">""", emotes);
    }

    [Fact]
    public async Task GetLastFmNowPlayingHtml_ReturnsBoardWhenRecentTrackExists()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(new EfUser { UserId = 10, Username = "User", Discriminator = "0001", Level = 1, Xp = 0 });
            db.Lastfms.Add(new Lastfm { UserId = 10, Lastfm1 = "listener" });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var command = CreateCommand(factory, CreateHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("""
            {
              "recenttracks": {
                "@attr": { "page": "1", "totalPages": "1", "user": "listener", "total": "1", "perPage": "2" },
                "track": [
                  {
                    "@attr": { "nowplaying": "true" },
                    "mbid": null,
                    "loved": null,
                    "artist": { "url": null, "mbid": null, "#text": "Artist" },
                    "image": [{ "#text": "small.png", "size": "small" }, { "#text": "large.png", "size": "large" }],
                    "date": null,
                    "url": "https://last.fm/now",
                    "name": "Track",
                    "album": { "mbid": null, "#text": "Album" }
                  }
                ]
              }
            }
            """)
        }));
        var profile = DefaultProfile(10);

        var result = await InvokePrivateInstanceAsync<Maybe<LastFmHtmlBoardResult>>(
            command,
            "GetLastFmNowPlayingHtml",
            profile,
            "api-key");

        Assert.True(result.HasValue);
        Assert.Contains("Track", result.Value.LastFmHtml);
        Assert.Contains("Artist", result.Value.LastFmHtml);
        Assert.Contains("large.png", result.Value.LastFmHtml);
        Assert.Equal(5, result.Value.LastFmTrackLength);
        Assert.Equal(6, result.Value.LastFmArtistLength);
    }

    [Fact]
    public async Task GenerateProfileHtml_RendersProfileSidebarAndLayoutData()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.Add(new EfUser { UserId = 10, Username = "User", Discriminator = "0001", Level = 4, Xp = 350 });
            db.Guilds.Add(new EfGuild
            {
                GuildId = 100,
                GuildName = "Guild",
                Prefix = "!",
                Icon = "guild.png",
                MemberCount = 1234,
                Leaderboard = true,
                Media = false,
                Tiktok = false
            });
            db.GuildMembers.Add(new EfGuildMember { GuildId = 100, UserId = 10, Level = 3, Xp = 250 });
            db.Bentos.Add(new Bento { UserId = 10, Bento1 = 42, BentoDate = DateTime.UtcNow });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var command = CreateCommand(factory);
        var profile = DefaultProfile(10) with
        {
            XpBoard = false,
            BackgroundUrl = "https://cdn.example/background.png",
            Description = "Custom description",
            Birthday = "February 18"
        };

        var html = await command.GenerateProfileHtml(
            profile,
            "lastfm-api-key",
            100,
            new ProfileDiscordUser(null, "0007", "DisplayName", "Nickname"),
            1234,
            "bot.png");

        Assert.Contains("https://cdn.example/background.png", html);
        Assert.Contains("Custom description", html);
        Assert.Contains("https://cdn.discordapp.com/embed/avatars/2.png", html);
        Assert.Contains("DisplayName", html);
        Assert.Contains("Nickname", html);
        Assert.Contains("Rank 1", html);
        Assert.Contains("Of 1234 Users", html);
        Assert.Contains("42 🍱", html);
        Assert.Contains("Feb 18 🎂", html);
        Assert.Contains("height: 365px", html);
        Assert.Contains("opacity: 0", html);
    }

    private static DomainProfile DefaultProfile(long userId) =>
        InvokePrivateStatic<DomainProfile>(typeof(ProfileCommands), "DefaultProfile", userId)!;

    private static ProfileCommands CreateCommand(
        InfrastructureTestDbFactory factory,
        HttpClient? lastFmClient = null,
        HttpClient? sushiiClient = null)
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return new ProfileCommands(
            new ProfileService(distributedCache, factory),
            new SushiiImageServerService(sushiiClient ?? CreateHttpClient(new HttpResponseMessage { StatusCode = HttpStatusCode.OK })),
            new LastFmCommands(
                new LastFmApiService(lastFmClient ?? CreateHttpClient(new HttpResponseMessage { StatusCode = HttpStatusCode.OK })),
                new SushiiImageServerService(sushiiClient ?? CreateHttpClient(new HttpResponseMessage { StatusCode = HttpStatusCode.OK }))),
            new LastFmService(factory),
            new UserService(memoryCache, factory),
            new GuildService(factory, memoryCache),
            new BentoService(memoryCache, factory));
    }

    private static HttpClient CreateHttpClient(HttpResponseMessage response)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return new HttpClient(mockHandler.Object);
    }
}
