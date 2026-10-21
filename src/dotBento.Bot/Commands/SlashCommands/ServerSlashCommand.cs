using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.CommandHandlers;
using dotBento.Bot.Extensions;
using dotBento.Bot.Services;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("server", "Commands for the Discord Server")]
public sealed class ServerSlashCommand(InteractiveService interactiveService, ServerCommand serverCommand, SettingsCommand settingsCommand, UserSettingService userSettingService, IDiscordGuildMemberResolver guildMembers, BotPresentation? presentation = null)
    : ConfiguredInteractionModule<SocketInteractionContext>(presentation)
{
    [SlashCommand("user", "Show info for a server user")]
    [GuildOnly]
    public async Task UserServerCommand([Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show server user info for you")] bool? hide = null)
    {
        user ??= Context.User;
        if (user.IsBot)
        {
            await user.ReturnIfBot(Context, interactiveService);
            return;
        }
        await DeferAsync(ephemeral: hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        var guildMember = await Context.RequireMemberAsync(guildMembers, user);
        if (guildMember.HasNoValue)
        {
            return;
        }
        await Context.SendFollowUpResponse(interactiveService, await serverCommand.UserServerCommand(guildMember.Value, Context.Guild), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [SlashCommand("info", "Show info for the server")]
    [GuildOnly]
    public async Task ServerCommand([Summary("hide", "Only show server info for you")] bool? hide = null) =>
        await Context.SendResponse(interactiveService, await serverCommand.ServerInfoCommand(Context.Guild),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));

    [SlashCommand("settings", "View and manage server settings")]
    [GuildOnly]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task SettingsCommand() =>
        await Context.SendResponse(interactiveService,
            await settingsCommand.GetServerSettingsAsync(
                (long)Context.Guild.Id, Context.Guild.Name, Context.Guild.IconUrl),
            true);
}
