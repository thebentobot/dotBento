using Discord.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Services;
using dotBento.Domain.Enums.Games;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[Name("Rps")]
public sealed class RpsTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    GameCommand gameCommand) : BaseCommandModule(botSettings)
{
    [Command("rps", RunMode = RunMode.Async)]
    [Summary("Play Rock Paper Scissors")]
    [Examples("rps rock", "rps paper", "rps scissors")]
    public async Task RpsCommand(string userChoice)
    {
        _ = Context.Channel.TriggerTypingAsync();
        if (!Enum.TryParse<RpsGameChoice>(userChoice, true, out var rpsChoice))
        {
            await Context.SendResponse(interactiveService, GenericEmbedService.ErrorEmbed("Error \ud83e\udea8 \ud83e\uddfb \u2702\ufe0f",
                "Please choose between rock \ud83e\udea8, paper \ud83e\uddfb or scissors \u2702\ufe0f"));
            return;
        }
        await Context.SendResponse(interactiveService, await gameCommand.RpsCommand(rpsChoice, (long)Context.User.Id));
    }
}