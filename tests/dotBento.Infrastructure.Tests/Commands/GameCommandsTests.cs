using dotBento.Domain.Enums.Games;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace dotBento.Infrastructure.Tests.Commands;

public sealed class GameCommandsTests
{
    [Theory]
    [InlineData(RpsGameChoice.Rock, RpsGameChoice.Scissors, RpsGameResult.Win)]
    [InlineData(RpsGameChoice.Rock, RpsGameChoice.Paper, RpsGameResult.Loss)]
    [InlineData(RpsGameChoice.Rock, RpsGameChoice.Rock, RpsGameResult.Draw)]
    public async Task RockPaperScissorsAsync_ReturnsExpectedResultAndPersistsStats(
        RpsGameChoice playerChoice,
        RpsGameChoice aiChoice,
        RpsGameResult expectedResult)
    {
        var factory = new InfrastructureTestDbFactory();
        var command = new GameCommands(new GameService(factory), (_, _) => (int)aiChoice);

        var (actualAiChoice, result) = await command.RockPaperScissorsAsync(playerChoice, 100);

        Assert.Equal(aiChoice, actualAiChoice);
        Assert.Equal(expectedResult, result);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var stats = await db.RpsGames.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        var total = (stats.RockWins ?? 0) + (stats.RockLosses ?? 0) + (stats.RockTies ?? 0);
        Assert.Equal(1, total);
    }

    [Fact]
    public void MagicEightBallResponse_ReturnsAResponse()
    {
        var response = GameCommands.MagicEightBallResponse();

        Assert.False(string.IsNullOrWhiteSpace(response));
    }

    [Fact]
    public void Roll_ReturnsValueWithinRange()
    {
        var roll = GameCommands.Roll(1, 7);

        Assert.InRange(roll, 1, 6);
    }
}
