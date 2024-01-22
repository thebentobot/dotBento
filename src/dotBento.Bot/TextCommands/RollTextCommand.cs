using Discord;
using Discord.Commands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Infrastructure.Commands;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.TextCommands;

[Name("Roll")]
public class RollTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    GameCommands gameCommands) : BaseCommandModule(botSettings)
{
    [Command("roll", RunMode = RunMode.Async)]
    [Summary("Roll a random number")]
    public async Task RollCommand([Summary("Minimum number, defaults to 1")] int? userMin = 1, [Summary("Maximum number, defaults to 10")] int? userMax = 10)
    {
        _ = Context.Channel.TriggerTypingAsync();
        var (min, max, failedValidation) = await ValidateUserInput(userMin, userMax);
        if (failedValidation) return;
        var result = gameCommands.Roll(min, max);
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
        await Context.SendResponse(interactiveService, embed);
    }

    private async Task<(int min, int max, bool failedValidation)> ValidateUserInput(int? userMin, int? userMax)
    {
        bool failedValidation = false;
        if (int.TryParse(userMin.ToString(), out var min).Equals(false))
        {
            var minNotANumberEmbed = new ResponseModel{ ResponseType = ResponseType.Embed };
            minNotANumberEmbed.Embed.WithTitle("The minimum number is not a valid number\nThe highest number I can roll is 1000")
                .WithColor(Color.Red);
            failedValidation = true;
            await Context.SendResponse(interactiveService, minNotANumberEmbed);
        }
        if (int.TryParse(userMax.ToString(), out var max).Equals(false))
        {
            var maxNotANumberEmbed = new ResponseModel{ ResponseType = ResponseType.Embed };
            maxNotANumberEmbed.Embed.WithTitle("The maximum number is not a valid number\nThe highest number I can roll is 1000")
                .WithColor(Color.Red);
            failedValidation = true;
            await Context.SendResponse(interactiveService, maxNotANumberEmbed);
        }
        if (min > max)
        {
            var minBiggerThanMaxEmbed = new ResponseModel{ ResponseType = ResponseType.Embed };
            minBiggerThanMaxEmbed.Embed.WithTitle("The minimum number cannot be greater than the maximum number")
                .WithColor(Color.Red);
            failedValidation = true;
            await Context.SendResponse(interactiveService, minBiggerThanMaxEmbed);
        }
        if (min > 1000 || max > 1000)
        {
            var minOrMaxTooBigEmbed = new ResponseModel{ ResponseType = ResponseType.Embed };
            minOrMaxTooBigEmbed.Embed.WithTitle("The minimum or maximum number cannot be greater than 1000")
                .WithColor(Color.Red);
            failedValidation = true;
            await Context.SendResponse(interactiveService, minOrMaxTooBigEmbed);
        }
        return (min, max, failedValidation);
    }
}