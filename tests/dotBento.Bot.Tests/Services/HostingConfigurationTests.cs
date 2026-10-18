using dotBento.Bot.Models;
using dotBento.Domain;
using Microsoft.Extensions.Configuration;

namespace dotBento.Bot.Tests.Services;

public sealed class HostingConfigurationTests
{
    [Fact]
    public void HostingOptions_BindOverridesAndPreserveDefaults()
    {
        var defaults = new BotEnvConfig();
        Assert.True(defaults.Logging.LokiEnabled);
        Assert.Equal("dotbento:", defaults.Cache.Prefix);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:LokiEnabled"] = "false", ["Cache:Prefix"] = "mochi:",
            ["Features:DonationRewardsEnabled"] = "false", ["Features:VotingRewardsEnabled"] = "false"
        }).Build();
        var bot = config.Get<BotEnvConfig>()!;
        Assert.False(bot.Logging.LokiEnabled);
        Assert.Equal("mochi:", bot.Cache.Prefix);
        Assert.False(bot.Features.DonationRewardsEnabled);
        Assert.False(bot.Features.VotingRewardsEnabled);
        Assert.Equal(bot.Cache.Prefix, config.GetSection("Cache").Get<CacheConfig>()!.Prefix);
        Assert.False(config.GetSection("Logging").Get<RemoteLoggingConfig>()!.LokiEnabled);
    }
}
