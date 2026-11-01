using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Services.Api;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace dotBento.Bot.Tests.Services;

public sealed class ImageRendererRegistrationTests
{
    [Fact]
    public void ImageRendererClientCanBeResolved()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddImageRenderer();

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ImageRendererService>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddImageRenderer_DoesNotRequireSizeOnSharedCache()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddImageRenderer();

        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IMemoryCache>();

        cache.Set("key", "value", TimeSpan.FromMinutes(1));

        Assert.Equal("value", cache.Get<string>("key"));
    }
}
