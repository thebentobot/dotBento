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
                userGame.RockWins++;
                break;
            case RpsGameChoice.Rock when result == RpsGameResult.Loss:
                userGame.RockLosses++;
                break;
            case RpsGameChoice.Rock:
                userGame.RockTies++;
                break;

            case RpsGameChoice.Paper when result == RpsGameResult.Win:
                userGame.PaperWins++;
                break;
            case RpsGameChoice.Paper when result == RpsGameResult.Loss:
                userGame.PaperLosses++;
                break;
            case RpsGameChoice.Paper:
                userGame.PaperTies++;
                break;

            case RpsGameChoice.Scissors when result == RpsGameResult.Win:
                userGame.ScissorWins++;
                break;
            case RpsGameChoice.Scissors when result == RpsGameResult.Loss:
                userGame.ScissorsLosses++;
                break;
            case RpsGameChoice.Scissors:
                userGame.ScissorsTies++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(choice), choice, null);
        }

        await context.SaveChangesAsync();
    }
}