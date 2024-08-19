using Discord.Commands;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[Name("Bento")]
public class BentoTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService, BentoCommand bentoCommand) : BaseCommandModule(botSettings)
{

    [Command("bento", RunMode = RunMode.Async)]
    [Summary("Give a friend a Bento Box or check your status")]
    [Examples("bento", "bento @Alonzo", "bento 188980576483540992")]
    public async Task BentoCommand(SocketUser? user = null)
    {
        _ = Context.Channel.TriggerTypingAsync();
        if (user == null)
        {
            await Context.SendResponse(interactiveService, await bentoCommand.CheckBentoCommand(Context.User));
            return;
        }
        await user.ReturnIfBot(Context, interactiveService);
        await Context.SendResponse(interactiveService, await bentoCommand.GiveBentoCommand(Context.User, user));
    }
}