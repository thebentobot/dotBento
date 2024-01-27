using Discord.Commands;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands
{
    [Name("User")]
    public class UserTextCommand(
        IOptions<BotEnvConfig> botSettings,
        InteractiveService interactiveService, UserCommand userCommand) : BaseCommandModule(botSettings)
    {

        [Command("user", RunMode = RunMode.Async)]
        [Summary("Show info for a user")]
        [Examples("user", "user @Lewis", "user 166142440233893888")]
        public async Task UserCommand(SocketUser? user = null)
        {
            _ = Context.Channel.TriggerTypingAsync();
            user ??= Context.User;
            await user.ReturnIfBot(Context, interactiveService);
            await Context.SendResponse(interactiveService, await userCommand.Command(user));
        }
    }
}