using Discord.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.TextCommands;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[Name("8ball")]
public class EightBallTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    GameCommand gameCommand) : BaseCommandModule(botSettings)
{
    [Command("8ball", RunMode = RunMode.Async)]
    [Summary("Ask the magic 8 ball a question")]
    [Examples("8ball Should I make a Bento")]
    public async Task EightBallCommand([Remainder] string question)
    {
        _ = Context.Channel.TriggerTypingAsync();
        await Context.SendResponse(interactiveService, await gameCommand.MagicEightBallCommand(question));
    }
}