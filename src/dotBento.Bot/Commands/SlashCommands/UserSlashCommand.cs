using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("user", "Commands for Discord Users")]
public class UserSlashCommand(InteractiveService interactiveService, UserCommand userCommand)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("info", "Show info for a user")]
    public async Task InfoCommand([Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show user info for you")] bool? hide = false)
    {
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        await Context.SendResponse(interactiveService, await userCommand.Command(user), hide ?? false);
    }

    [GuildOnly]
    [SlashCommand("profile", "Show a user's Bento profile")]
    public async Task ProfileCommand([Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show user info for you")]
        bool? hide = false)
    {
        _ = DeferAsync();
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var guildMember = Context.Guild.Users.FirstOrDefault(x => x.Id == user.Id);
        var botPfp = Context.Client.CurrentUser.GetDisplayAvatarUrl();
        await Context.SendFollowUpResponse(interactiveService,
            await userCommand.GetProfileAsync((long)user.Id,
                (long)Context.Guild.Id,
                guildMember,
                Context.Guild.MemberCount,
                botPfp),
            hide ?? false);
    }
}