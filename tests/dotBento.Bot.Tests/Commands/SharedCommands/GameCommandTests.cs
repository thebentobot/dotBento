using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Domain.Enums.Games;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services;

namespace dotBento.Bot.Tests.Commands.SharedCommands;

public sealed class GameCommandTests
{
    [Theory]
    [InlineData(null, null, "The minimum number is not a valid number")]
    [InlineData(10, null, "The maximum number is not a valid number")]
    [InlineData(10, 1, "The minimum number cannot be greater than the maximum number")]
    [InlineData(1, 1001, "The minimum or maximum number cannot be greater than 1000")]
    public async Task RollCommand_ReturnsValidationErrors(int? min, int? max, string expectedTitle)
    {
        var response = await GameCommand.RollCommand(min, max);

        Assert.Equal(ResponseType.Embed, response.ResponseType);
        Assert.StartsWith(expectedTitle, response.Embed.Build().Title);
    }

    [Fact]
    public async Task RollCommand_ReturnsRollEmbedForValidInput()
    {
        var response = await GameCommand.RollCommand(1, 2);
        var embed = response.Embed.Build();

        Assert.Equal(ResponseType.Embed, response.ResponseType);
        Assert.StartsWith("And the number is...", embed.Title);
        Assert.Equal("Rolled between 1 and 2", embed.Author!.Value.Name);
    }

    [Fact]
    public async Task MagicEightBallCommand_ReturnsQuestionEmbed()
    {
        var response = await GameCommand.MagicEightBallCommand("Will this pass?");

        Assert.Equal("\"Will this pass?\"", response.Embed.Build().Title);
    }

    [Fact]
    public async Task RpsCommand_FormatsWinResponse()
    {
        var factory = new TestDbFactory();
        var gameCommands = new GameCommands(new GameService(factory), (_, _) => (int)RpsGameChoice.Scissors);
        var command = new GameCommand(gameCommands);

        var response = await command.RpsCommand(RpsGameChoice.Rock, 10);

        Assert.Contains("You chose", response.Embed.Build().Description);
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, db.RpsGames.Single().RockWins);
    }
}
