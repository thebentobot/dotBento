using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("user", "Commmands for Discord Users")]
public class UserSlashCommand(InteractiveService interactiveService, UserCommand userCommand)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("info", "Show info for a user")]
    public async Task UserCommand([Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show user info for you")] bool? hide = false)
    {
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        await Context.SendResponse(interactiveService, await userCommand.Command(user), hide ?? false);
    }
}