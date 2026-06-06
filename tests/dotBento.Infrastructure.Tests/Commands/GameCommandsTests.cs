using dotBento.Domain.Enums.Games;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace dotBento.Infrastructure.Tests.Commands;

public sealed class GameCommandsTests
{
    [Fact]
    public async Task RockPaperScissorsAsync_ReturnsValidAiChoiceAndPersistsOneResult()
    {
        var factory = new InfrastructureTestDbFactory();
        var command = new GameCommands(new GameService(factory));

        var (aiChoice, result) = await command.RockPaperScissorsAsync(RpsGameChoice.Rock, 100);

        Assert.Contains(aiChoice, new[]
        {
            RpsGameChoice.Rock,
            RpsGameChoice.Paper,
            RpsGameChoice.Scissors
        });
        Assert.Contains(result, new[]
        {
            RpsGameResult.Win,
            RpsGameResult.Loss,
            RpsGameResult.Draw
        });

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
