using dotBento.Domain.Entities;
using dotBento.Domain.Enums.Games;
using dotBento.Infrastructure.Services;

namespace dotBento.Infrastructure.Commands;

public sealed class GameCommands(GameService gameService)
{
    public async Task<(RpsGameChoice, RpsGameResult)> RockPaperScissorsAsync(RpsGameChoice playerChoice, long userId)
    {
        var random = new Random();
        var aiChoice = (RpsGameChoice) random.Next(0, 3);

        RpsGameResult result;

        switch (playerChoice)
        {
            case RpsGameChoice.Rock when aiChoice == RpsGameChoice.Paper:
            case RpsGameChoice.Paper when aiChoice == RpsGameChoice.Scissors:
            case RpsGameChoice.Scissors when aiChoice == RpsGameChoice.Rock:
                result = RpsGameResult.Loss;
                break;

            case RpsGameChoice.Rock when aiChoice == RpsGameChoice.Scissors:
            case RpsGameChoice.Paper when aiChoice == RpsGameChoice.Rock:
            case RpsGameChoice.Scissors when aiChoice == RpsGameChoice.Paper:
                result = RpsGameResult.Win;
                break;

            default:
                result = RpsGameResult.Draw;
                break;
        }

        await gameService.UpdateRpsStatsAsync(userId, playerChoice, result);

        return (aiChoice, result);
    }
    
    public static string MagicEightBallResponse() => MagicEightBall.RandomResponse();
    
    public static int Roll(int min, int max)
    {
        var random = new Random();
        return random.Next(min, max);
    }
}