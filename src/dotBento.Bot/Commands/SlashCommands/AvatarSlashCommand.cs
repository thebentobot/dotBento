using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("avatar", "Get the avatar of a user")]
public sealed class AvatarSlashCommand(InteractiveService interactiveService, AvatarCommand avatarCommand) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("user", "Get the avatar of a User Profile")]
    public async Task UserAvatarCommand(
        [Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show avatar for you")] bool? hide = false
        )
    {
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        await Context.SendResponse(interactiveService, await avatarCommand.UserAvatarCommand(user), hide ?? false);
    }
    
    [GuildOnly]
    [SlashCommand("server", "Get the avatar of a Server Profile")]
    public async Task ServerAvatarCommand(
        [Summary("user", "Pick a User")] SocketGuildUser? user = null,
        [Summary("hide", "Only show avatar for you")] bool? hide = false
    )
    {
        user ??= Context.Guild.Users.Single(x => x.Id == Context.User.Id);
        await user.ReturnIfBot(Context, interactiveService);
        await Context.SendResponse(interactiveService, await avatarCommand.ServerAvatarCommand(user), hide ?? false);
    }
}