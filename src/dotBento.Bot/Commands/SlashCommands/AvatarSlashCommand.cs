using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.CommandHandlers;
using dotBento.Bot.Extensions;
using dotBento.Bot.Services;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("avatar", "Get the avatar of a user")]
public sealed class AvatarSlashCommand(InteractiveService interactiveService, AvatarCommand avatarCommand, UserSettingService userSettingService, IDiscordGuildMemberResolver guildMembers, BotPresentation? presentation = null) : ConfiguredInteractionModule<SocketInteractionContext>(presentation)
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
        await DeferAsync(ephemeral: hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        var member = await Context.RequireMemberAsync(guildMembers, user);
        if (member.HasNoValue) return;
        var guildUser = member.Value;

        await Context.SendFollowUpResponse(interactiveService, await avatarCommand.ServerAvatarCommand(guildUser), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }
}
