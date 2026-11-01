using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Services.Api;
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
}
