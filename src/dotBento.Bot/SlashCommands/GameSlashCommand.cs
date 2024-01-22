using Discord;
using Discord.Interactions;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Domain.Enums.Games;
using dotBento.Domain.Extensions.Games;
using dotBento.Infrastructure.Commands;
using Fergun.Interactive;

namespace dotBento.Bot.SlashCommands;

[Group("game", "Play a game")]
public class GameSlashCommand(InteractiveService interactiveService, GameCommands gameCommands) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("rps", "Play Rock Paper Scissors")]
    public async Task RpsCommand(
        [Summary("weapon", "Do you pick rock, paper or scissors?")] RpsGameChoice choice,
        [Summary("hide", "Only show the result for you")] bool? hide = false
    )
    {
        var (aiChoice, result) = await gameCommands.RockPaperScissorsAsync(choice, (long)Context.User.Id);
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle("Rock Paper Scissors \ud83e\udea8 \ud83e\uddfb \u2702\ufe0f")
            .WithDescription($"You chose **{choice.AddEmoji()}** and I chose **{aiChoice.AddEmoji()}** {result.FormatResult()}")
            .WithColor(result switch
            {
                RpsGameResult.Win => Color.Green,
                RpsGameResult.Loss => Color.Red,
                _ => Color.Blue
            });
        await Context.SendResponse(interactiveService, embed, hide ?? false);
    }
    
    [SlashCommand("8ball", "Ask the magic 8 ball a question")]
    public async Task EightBallCommand(
        [Summary("question", "Ask the magic 8 ball a question")] string question,
        [Summary("hide", "Only show the result for you")] bool? hide = false
    )
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var embedAuthor = new EmbedAuthorBuilder()
            .WithName("Magic 8 Ball")
            .WithIconUrl("https://upload.wikimedia.org/wikipedia/commons/thumb/e/e3/8_ball_icon.svg/1200px-8_ball_icon.svg.png");
        embed.Embed.WithTitle($"\"{question}\"")
            .WithAuthor(embedAuthor)
            .WithDescription($"{gameCommands.MagicEightBallResponse()}")
            .WithColor(0, 0, 0);
        await Context.SendResponse(interactiveService, embed, hide ?? false);
    }
    
    [SlashCommand("roll", "Roll a random number")]
    public async Task RollCommand(
        [Summary("min", "The minimum number to roll (default = 1)")] int? min = 1,
        [Summary("max", "The maximum number to roll (default = 10)")] int? max = 10,
        [Summary("hide", "Only show the result for you")] bool? hide = false
    )
    {
        if (min > max)
        {
            var minBiggerThanMaxEmbed = new ResponseModel{ ResponseType = ResponseType.Embed };
            minBiggerThanMaxEmbed.Embed.WithTitle("The minimum number cannot be greater than the maximum number")
                .WithColor(Color.Red);
            await Context.SendResponse(interactiveService, minBiggerThanMaxEmbed, hide ?? false);
        }
        var result = gameCommands.Roll(min ?? 1, max ?? 10);
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
        await Context.SendResponse(interactiveService, embed, hide ?? false);
    }
}