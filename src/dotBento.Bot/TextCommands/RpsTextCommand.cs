using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Domain.Enums;
using dotBento.Domain.Enums.Games;
using dotBento.Domain.Extensions.Games;
using dotBento.Infrastructure.Commands;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.TextCommands;

[Name("Rps")]
public class RpsTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    GameCommands gameCommands) : BaseCommandModule(botSettings)
{
    [Command("rps", RunMode = RunMode.Async)]
    [Summary("Play Rock Paper Scissors")]
    public async Task RpsCommand([Summary("Choose between rock, paper, scissors")] string userChoice)
    {
        _ = Context.Channel.TriggerTypingAsync();
        if (!Enum.TryParse<RpsGameChoice>(userChoice, true, out var rpsChoice))
        {
            var errorEmbed = new ResponseModel{ ResponseType = ResponseType.Embed };
            errorEmbed.Embed.WithTitle("Error \ud83e\udea8 \ud83e\uddfb \u2702\ufe0f")
                .WithDescription("Please choose between rock \ud83e\udea8, paper \ud83e\uddfb or scissors \u2702\ufe0f")
                .WithColor(Color.Red);
            await Context.SendResponse(interactiveService, errorEmbed);
            return;
        }
        var (aiChoice, result) = await gameCommands.RockPaperScissorsAsync(rpsChoice, (long)Context.User.Id);
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle("Rock Paper Scissors \ud83e\udea8 \ud83e\uddfb \u2702\ufe0f")
            .WithDescription($"You chose **{rpsChoice.AddEmoji()}** and I chose **{aiChoice.AddEmoji()}** {result.FormatResult()}")
            .WithColor(result switch
            {
                RpsGameResult.Win => Color.Green,
                RpsGameResult.Loss => Color.Red,
                _ => Color.Blue
            });
        await Context.SendResponse(interactiveService, embed);
    }
}