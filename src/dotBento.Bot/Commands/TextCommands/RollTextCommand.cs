using Discord.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[Name("Roll")]
public sealed class RollTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService) : BaseCommandModule(botSettings)
{
    [Command("roll", RunMode = RunMode.Async)]
    [Summary("Roll a random number. Defaults to 1-10, min 1, max 1000")]
    [Examples("roll", "roll 1 10", "roll 1 1000")]
    public async Task RollCommand(int? userMin = 1, int? userMax = 10)
    {
        _ = Context.Channel.TriggerTypingAsync();
        await Context.SendResponse(interactiveService, await GameCommand.RollCommand(userMin, userMax));
    }
}