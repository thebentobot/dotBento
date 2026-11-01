using dotBento.Bot.Services;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.CommandHandlers;
using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("user", "Commands for Discord Users")]
public sealed class UserSlashCommand(InteractiveService interactiveService, UserCommand userCommand, SettingsCommand settingsCommand, UserSettingService userSettingService, IDiscordGuildMemberResolver guildMembers, BotPresentation? presentation = null)
    : ConfiguredInteractionModule<SocketInteractionContext>(presentation)
{
    [SlashCommand("info", "Show info for a user")]
    public async Task InfoCommand([Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show user info for you")] bool? hide = null)
    {
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        await Context.SendResponse(interactiveService, await userCommand.Command(user), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [GuildOnly]
    [SlashCommand("profile", "Show a user's Bento profile")]
    public async Task ProfileCommand([Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show user info for you")]
        bool? hide = null)
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
        var guildMember = member.Value;
        var botPfp = Context.Client.CurrentUser.GetDisplayAvatarUrl();
        await Context.SendFollowUpResponse(interactiveService,
            await userCommand.GetProfileAsync((long)user.Id,
                (long)Context.Guild.Id,
                guildMember,
                Context.Guild.MemberCount,
                botPfp),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [SlashCommand("settings", "View, manage, or delete your personal settings and data")]
    [OptOutExempt]
    public async Task SettingsCommand() =>
        await Context.SendResponse(interactiveService,
            await settingsCommand.GetUserSettingsAsync((long)Context.User.Id),
            true);
}
