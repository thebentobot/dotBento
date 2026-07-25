using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Services;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("avatar", "Get the avatar of a user")]
public sealed class AvatarSlashCommand(InteractiveService interactiveService, AvatarCommand avatarCommand, UserSettingService userSettingService) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("user", "Get the avatar of a User Profile")]
    public async Task UserAvatarCommand(
        [Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show avatar for you")] bool? hide = null
        )
    {
        user ??= Context.User;
        if (user.IsBot)
        {
            await user.ReturnIfBot(Context, interactiveService);
            return;
        }

        await Context.SendResponse(interactiveService, await avatarCommand.UserAvatarCommand(user), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [GuildOnly]
    [SlashCommand("server", "Get the avatar of a Server Profile")]
    public async Task ServerAvatarCommand(
        [Summary("user", "Pick a User")] SocketGuildUser? user = null,
        [Summary("hide", "Only show avatar for you")] bool? hide = null
    )
    {
        user ??= Context.Guild.GetUser(Context.User.Id);
        if (user is null)
        {
            await Context.SendResponse(
                interactiveService,
                GenericEmbedService.ErrorEmbed("User unavailable", "Could not resolve your server profile."),
                true);
            return;
        }

        if (user.IsBot)
        {
            await user.ReturnIfBot(Context, interactiveService);
            return;
        }

        await Context.SendResponse(interactiveService, await avatarCommand.ServerAvatarCommand(user), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }
}
