using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

public class BentoSlashCommand(InteractiveService interactiveService, BentoCommand bentoCommand)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("bento", "Give a friend a Bento Box")]
    public async Task BentoCommand([Summary("user", "Pick a User. Omit to check status")] SocketUser? user = null,
        [Summary("hide", "Only show result for you")] bool? hide = false)
    {
        if (user == null)
        {
            await Context.SendResponse(interactiveService, await bentoCommand.CheckBentoCommand(Context.User), hide ?? false);
            return;
        }
        await user.ReturnIfBot(Context, interactiveService);
        await Context.SendResponse(interactiveService, await bentoCommand.GiveBentoCommand(Context.User, user), hide ?? false);
    }
}