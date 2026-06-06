using dotBento.Bot.Services;

namespace dotBento.Bot.Tests.Services;

public sealed class GenericEmbedServiceTests
{
    [Fact]
    public void ErrorEmbed_BuildsErrorResponse()
    {
        var response = GenericEmbedService.ErrorEmbed("Nope", "Details");
        var embed = response.Embed.Build();

        Assert.Equal("Nope", embed.Title);
        Assert.Equal("Details", embed.Description);
    }

}
