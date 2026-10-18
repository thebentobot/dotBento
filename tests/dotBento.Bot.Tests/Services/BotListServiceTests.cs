using dotBento.Bot.Models;
using dotBento.Bot.Services;

namespace dotBento.Bot.Tests.Services;

public sealed class BotListServiceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void BotListPayload_IncludesOnlyConfiguredProviders(int mask)
    {
        var config = new BotListConfig
        {
            TopGgApiToken = (mask & 1) != 0 ? "top-token" : "",
            DiscordBotsGgToken = (mask & 2) != 0 ? "gg-token" : "  ",
            DiscordBotListToken = (mask & 4) != 0 ? "list-token" : ""
        };
        var providers = BotListService.ConfiguredProviders(config);
        var payload = BotListService.BuildPayload(42, 123456789012345678, providers);
        Assert.Equal((mask & 1) != 0, payload.ContainsKey("top.gg"));
        Assert.Equal((mask & 2) != 0, payload.ContainsKey("discord.bots.gg"));
        Assert.Equal((mask & 4) != 0, payload.ContainsKey("discordbotlist.com"));
        if ((mask & 1) != 0) Assert.Equal("top-token", payload["top.gg"]);
        if ((mask & 2) != 0) Assert.Equal("gg-token", payload["discord.bots.gg"]);
        if ((mask & 4) != 0) Assert.Equal("list-token", payload["discordbotlist.com"]);
        Assert.Equal(42, payload["server_count"]);
        Assert.Equal("123456789012345678", payload["bot_id"]);
        Assert.Equal(providers.Count + 2, payload.Count);
    }
}
