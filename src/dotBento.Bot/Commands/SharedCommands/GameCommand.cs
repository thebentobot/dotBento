using Discord;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Domain.Enums.Games;
using dotBento.Domain.Extensions.Games;
using dotBento.Infrastructure.Commands;

namespace dotBento.Bot.Commands.SharedCommands;

public sealed class GameCommand(GameCommands gameCommands)
{
    public async Task<ResponseModel> RpsCommand(RpsGameChoice choice, long userId)
    {
        var (aiChoice, result) = await gameCommands.RockPaperScissorsAsync(choice, userId);
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle("Rock Paper Scissors \ud83e\udea8 \ud83e\uddfb \u2702\ufe0f")
            .WithDescription($"You chose **{choice.AddEmoji()}** and I chose **{aiChoice.AddEmoji()}** {result.FormatResult()}")
            .WithColor(result switch
            {
                RpsGameResult.Win => Color.Green,
                RpsGameResult.Loss => Color.Red,
                _ => Color.Blue
            });
        return embed;
    }

    public static Task<ResponseModel> MagicEightBallCommand(string question)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var embedAuthor = new EmbedAuthorBuilder()
            .WithName("Magic 8 Ball")
            .WithIconUrl("https://upload.wikimedia.org/wikipedia/commons/thumb/e/e3/8_ball_icon.svg/1200px-8_ball_icon.svg.png");
        embed.Embed.WithTitle($"\"{question}\"")
            .WithAuthor(embedAuthor)
            .WithDescription($"{GameCommands.MagicEightBallResponse()}")
            .WithColor(0, 0, 0);
        return Task.FromResult(embed);
    }
    
    public static Task<ResponseModel> RollCommand(int? userMin, int? userMax)
    {
        var (min, max, failedValidation, error) = ValidateUserInput(userMin, userMax).Result;
        if (failedValidation) return Task.FromResult(error);
        var result = GameCommands.Roll(min, max);
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var embedAuthor = new EmbedAuthorBuilder()
            .WithName($"Rolled between {min} and {max}")
            .WithIconUrl("https://pngimg.com/d/dice_PNG41.png");
        var embedFooter = new EmbedFooterBuilder()
            .WithText($"The chance of rolling {result} is {(1.0 / (max - min + 1)) * 100}%");
        embed.Embed.WithTitle($"And the number is... {result}")
            .WithAuthor(embedAuthor)
            .WithColor(DiscordConstants.BentoYellow)
            .WithFooter(embedFooter);
        return Task.FromResult(embed);
    }
    
    private static Task<(int min, int max, bool failedValidation, ResponseModel error)> ValidateUserInput(int? userMin, int? userMax)
    {
        var errorEmbed = new ResponseModel { ResponseType = ResponseType.Embed };
        if (int.TryParse(userMin.ToString(), out var min).Equals(false))
        {
            errorEmbed.Embed.WithTitle("The minimum number is not a valid number\nThe highest number I can roll is 1000 and the lowest is 1")
                .WithColor(Color.Red);
            return Task.FromResult((0, 0, true, errorEmbed));
        }
        if (int.TryParse(userMax.ToString(), out var max).Equals(false))
        {
            errorEmbed.Embed.WithTitle("The maximum number is not a valid number\nThe highest number I can roll is 1000 and the lowest is 1")
                .WithColor(Color.Red);
            return Task.FromResult((0, 0, true, errorEmbed));
        }
        if (min > max)
        {
            errorEmbed.Embed.WithTitle("The minimum number cannot be greater than the maximum number")
                .WithColor(Color.Red);
            return Task.FromResult((0, 0, true, errorEmbed));
        }

        if (min <= 1000 && max <= 1000) return Task.FromResult<(int min, int max, bool failedValidation, ResponseModel error)>((min, max, false, errorEmbed));
        errorEmbed.Embed.WithTitle("The minimum or maximum number cannot be greater than 1000")
            .WithColor(Color.Red);
        return Task.FromResult((0, 0, true, errorEmbed));
    }
}