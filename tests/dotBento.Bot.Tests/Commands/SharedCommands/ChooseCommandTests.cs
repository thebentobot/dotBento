using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;

namespace dotBento.Bot.Tests.Commands.SharedCommands;

public sealed class ChooseCommandTests
{
    [Fact]
    public async Task Command_RequiresCommaSeparatedOptions()
    {
        var response = await ChooseCommand.Command("only one option");

        Assert.Equal(ResponseType.Embed, response.ResponseType);
        Assert.Equal("You need to separate options with commas", response.Embed.Build().Title);
    }

    [Fact]
    public async Task Command_RejectsMoreThanTwentyOptions()
    {
        var response = await ChooseCommand.Command(string.Join(",", Enumerable.Range(1, 21)));

        Assert.Equal("You need to provide less than 20 options", response.Embed.Build().Title);
    }

    [Fact]
    public async Task Command_ReturnsChoiceForValidOptions()
    {
        var response = await ChooseCommand.Command("tea,coffee");
        var embed = response.Embed.Build();

        Assert.Equal(ResponseType.Embed, response.ResponseType);
        Assert.Equal("I choose...", embed.Title);
        Assert.True(embed.Description is "**tea**" or "**coffee**");
    }
}
