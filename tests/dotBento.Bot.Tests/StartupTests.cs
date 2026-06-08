using dotBento.Bot;
using NetCord.Gateway;

namespace dotBento.Bot.Tests;

public sealed class StartupTests
{
    [Fact]
    public void CreateGatewayClientConfiguration_ConfiguresConcurrentCache()
    {
        var configuration = Startup.CreateGatewayClientConfiguration();

        Assert.Same(ConcurrentGatewayClientCacheProvider.Empty, configuration.CacheProvider);
    }
}
