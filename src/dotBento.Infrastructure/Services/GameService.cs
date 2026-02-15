using dotBento.Domain.Enums.Games;
using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using dotBento.EntityFramework.Entities;

namespace dotBento.Infrastructure.Services;

public sealed class GameService(IDbContextFactory<BotDbContext> contextFactory)
{
    public async Task UpdateRpsStatsAsync(long userId, RpsGameChoice choice, RpsGameResult result)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var userGame = await context.RpsGames.FirstOrDefaultAsync(x => x.UserId == userId);

        if (userGame == null)
        {
            userGame = new RpsGame
            {
                UserId = userId
            };
            context.RpsGames.Add(userGame);
        }

        switch (choice)
        {
            case RpsGameChoice.Rock when result == RpsGameResult.Win:
                userGame.RockWins = (userGame.RockWins ?? 0) + 1;
                break;
            case RpsGameChoice.Rock when result == RpsGameResult.Loss:
                userGame.RockLosses = (userGame.RockLosses ?? 0) + 1;
                break;
            case RpsGameChoice.Rock:
                userGame.RockTies = (userGame.RockTies ?? 0) + 1;
                break;

            case RpsGameChoice.Paper when result == RpsGameResult.Win:
                userGame.PaperWins = (userGame.PaperWins ?? 0) + 1;
                break;
            case RpsGameChoice.Paper when result == RpsGameResult.Loss:
                userGame.PaperLosses = (userGame.PaperLosses ?? 0) + 1;
                break;
            case RpsGameChoice.Paper:
                userGame.PaperTies = (userGame.PaperTies ?? 0) + 1;
                break;

            case RpsGameChoice.Scissors when result == RpsGameResult.Win:
                userGame.ScissorWins = (userGame.ScissorWins ?? 0) + 1;
                break;
            case RpsGameChoice.Scissors when result == RpsGameResult.Loss:
                userGame.ScissorsLosses = (userGame.ScissorsLosses ?? 0) + 1;
                break;
            case RpsGameChoice.Scissors:
                userGame.ScissorsTies = (userGame.ScissorsTies ?? 0) + 1;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(choice), choice, null);
        }

        await context.SaveChangesAsync();
    }
}