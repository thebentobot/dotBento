using System.Net;
using System.Text.Json;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Models;
using dotBento.Infrastructure.Services;
using dotBento.Infrastructure.Services.Api;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Tests.Commands.SharedCommands;

public sealed class HoroscopeCommandTests
{
    [Theory]
    [InlineData("yesterday", "Aug 7, 2026", "Yesterday")]
    [InlineData("today", "Aug 8, 2026", "Today")]
    [InlineData("tomorrow", "Aug 9, 2026", "Tomorrow")]
    [InlineData("weekly", "Aug 3, 2026 - Aug 9, 2026", "This week")]
    [InlineData("monthly", "August 2026", "This month")]
    public async Task GetHoroscopeAsync_AllPeriods_RequestExpectedWindowAndRenderTemporalLabel(
        string window,
        string dateLabel,
        string temporalLabel)
    {
        var (command, handler, _) = CreateCommand(responseWindow: window, dateLabel: dateLabel);

        var response = await command.GetHoroscopeAsync(10, window, "libra");

        Assert.Equal(ResponseType.Embed, response.ResponseType);
        Assert.EndsWith($"/horoscope/libra?window={window}", Assert.Single(handler.RequestUris));
        Assert.Equal($"♎ Libra | {temporalLabel} · {dateLabel}", response.Embed.Build().Title);
    }

    [Theory]
    [InlineData("week", "weekly")]
    [InlineData("weekly", "weekly")]
    [InlineData("month", "monthly")]
    [InlineData("monthly", "monthly")]
    public void NormalizeWindow_TextAliases_ReturnCanonicalPeriod(string input, string expected)
    {
        Assert.Equal(expected, HoroscopeCommand.NormalizeWindow(input));
    }

    [Fact]
    public async Task GetHoroscopeAsync_UsesSavedSignAndAllowsOneOffOverride()
    {
        var (command, handler, factory) = CreateCommand();
        await new HoroscopeService(factory).SaveHoroscopeAsync(10, "taurus");

        await command.GetHoroscopeAsync(10, "today");
        await command.GetHoroscopeAsync(10, "today", "libra");

        Assert.Contains("/horoscope/taurus?window=today", handler.RequestUris[0]);
        Assert.Contains("/horoscope/libra?window=today", handler.RequestUris[1]);
        Assert.Equal("taurus", (await new HoroscopeService(factory).GetHoroscopeAsync(10)).Value.Sign);
    }

    [Fact]
    public async Task GetHoroscopeAsync_RendersPurpleEmbedAndNonInlineAspectStars()
    {
        var (command, _, _) = CreateCommand(aspects: true);

        var response = await command.GetHoroscopeAsync(10, "today", "libra");
        var embed = response.Embed.Build();

        Assert.Equal((uint)0x9266CC, embed.Color!.Value.RawValue);
        Assert.Equal("♎ Libra | Today · Aug 8, 2026", embed.Title);
        Assert.Equal("Choose balance.", embed.Description);
        var field = Assert.Single(embed.Fields);
        Assert.Equal("Romance ★★★★☆", field.Name);
        Assert.Equal("Be open.", field.Value);
        Assert.False(field.Inline);
    }

    [Fact]
    public async Task GetHoroscopeAsync_LongReadingReturnsBoundedPaginator()
    {
        var reading = string.Join(' ', Enumerable.Repeat("forward", 1800));
        var (command, _, _) = CreateCommand(reading: reading, aspects: true);

        var response = await command.GetHoroscopeAsync(10, "today", "aries");

        Assert.Equal(ResponseType.Paginator, response.ResponseType);
        var pages = response.StaticPaginator!.Pages.ToList();
        Assert.True(pages.Count > 1);
        Assert.All(pages, page => Assert.InRange(page.GetEmbedArray()[0].Description.Length, 1, 4096));
        Assert.All(pages, page => Assert.Equal("♎ Libra | Today · Aug 8, 2026", page.GetEmbedArray()[0].Title));
        Assert.Empty(pages[0].GetEmbedArray()[0].Fields);
        Assert.Single(pages[^1].GetEmbedArray()[0].Fields);
    }

    [Fact]
    public async Task GetHoroscopeAsync_ReturnsClearValidationConfigurationAndUpstreamErrors()
    {
        var (configured, _, _) = CreateCommand(statusCode: HttpStatusCode.BadGateway);
        var (unconfigured, unconfiguredHandler, _) = CreateCommand(configured: false);

        var invalid = await configured.GetHoroscopeAsync(10, "today", "not-a-sign");
        var missing = await configured.GetHoroscopeAsync(10, "today");
        var noConfig = await unconfigured.GetHoroscopeAsync(10, "today", "aries");
        var upstream = await configured.GetHoroscopeAsync(10, "today", "aries");

        Assert.Equal("Invalid zodiac sign", invalid.Embed.Build().Title);
        Assert.Equal("No zodiac sign saved", missing.Embed.Build().Title);
        Assert.Equal("Horoscope commands are not enabled", noConfig.Embed.Build().Title);
        Assert.Empty(unconfiguredHandler.RequestUris);
        Assert.Equal("Failed to fetch horoscope", upstream.Embed.Build().Title);
    }

    [Fact]
    public async Task SaveRemoveAndListHoroscopes_ExposeExpectedUserExperience()
    {
        var (command, _, factory) = CreateCommand();

        var saved = await command.SaveHoroscopeAsync(10, "PISCES");
        var listed = command.ListHoroscopes();
        var removed = await command.RemoveHoroscopeAsync(10);

        Assert.Contains("Pisces", saved.Embed.Build().Description);
        Assert.Contains("♈ **Aries**: Mar 21 – Apr 19", listed.Embed.Build().Description);
        Assert.Contains("♓ **Pisces**: Feb 19 – Mar 20", listed.Embed.Build().Description);
        Assert.Equal("Zodiac sign removed", removed.Embed.Build().Title);
        Assert.True((await new HoroscopeService(factory).GetHoroscopeAsync(10)).HasNoValue);
    }

    private static (HoroscopeCommand Command, RecordingHandler Handler, TestDbFactory Factory) CreateCommand(
        string reading = "Choose balance.",
        bool aspects = false,
        bool configured = true,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string responseWindow = "today",
        string dateLabel = "Aug 8, 2026")
    {
        var handler = new RecordingHandler(() => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                zodiac = "libra",
                window = responseWindow,
                label = dateLabel,
                reading,
                aspects = aspects
                    ? new[] { new { name = "Romance", score = 4, detail = "Be open." } }
                    : []
            }))
        });
        var factory = new TestDbFactory();
        var command = new HoroscopeCommand(
            new HoroscopeService(factory),
            new BentoMediaServerService(new HttpClient(handler)),
            Options.Create(new BotEnvConfig
            {
                MediaServer = new MediaServerConfig
                {
                    Url = configured ? "https://media.example" : string.Empty,
                    ApiKey = "secret"
                }
            }));
        return (command, handler, factory);
    }

    private sealed class RecordingHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public List<string> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!.ToString());
            Assert.Equal("secret", request.Headers.GetValues("X-API-Key").Single());
            return Task.FromResult(responseFactory());
        }
    }
}
