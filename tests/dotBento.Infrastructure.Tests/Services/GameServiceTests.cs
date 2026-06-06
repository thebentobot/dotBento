using dotBento.Domain.Enums.Games;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace dotBento.Infrastructure.Tests.Services;

public class GameServiceTests
{
    public static IEnumerable<object[]> RpsCases() =>
    new List<object[]>
    {
        new object[] { RpsGameChoice.Rock, RpsGameResult.Win, nameof(EntityFramework.Entities.RpsGame.RockWins) },
        new object[] { RpsGameChoice.Rock, RpsGameResult.Loss, nameof(EntityFramework.Entities.RpsGame.RockLosses) },
        new object[] { RpsGameChoice.Rock, RpsGameResult.Draw, nameof(EntityFramework.Entities.RpsGame.RockTies) },
        new object[] { RpsGameChoice.Paper, RpsGameResult.Win, nameof(EntityFramework.Entities.RpsGame.PaperWins) },
        new object[] { RpsGameChoice.Paper, RpsGameResult.Loss, nameof(EntityFramework.Entities.RpsGame.PaperLosses) },
        new object[] { RpsGameChoice.Paper, RpsGameResult.Draw, nameof(EntityFramework.Entities.RpsGame.PaperTies) },
        new object[] { RpsGameChoice.Scissors, RpsGameResult.Win, nameof(EntityFramework.Entities.RpsGame.ScissorWins) },
        new object[] { RpsGameChoice.Scissors, RpsGameResult.Loss, nameof(EntityFramework.Entities.RpsGame.ScissorsLosses) },
        new object[] { RpsGameChoice.Scissors, RpsGameResult.Draw, nameof(EntityFramework.Entities.RpsGame.ScissorsTies) }
    };

    [Theory]
    [MemberData(nameof(RpsCases))]
    public async Task UpdateRpsStatsAsync_IncrementsExpectedColumn(
        RpsGameChoice choice,
        RpsGameResult result,
        string propertyName)
    {
        var factory = new InfrastructureTestDbFactory();
        var service = new GameService(factory);

        await service.UpdateRpsStatsAsync(10, choice, result);
        await service.UpdateRpsStatsAsync(10, choice, result);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var game = await db.RpsGames.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        var value = typeof(EntityFramework.Entities.RpsGame).GetProperty(propertyName)!.GetValue(game);
        Assert.Equal(2, value);
    }
}
