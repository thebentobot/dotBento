using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("server", "Commands for the Discord Server")]
public sealed class ServerSlashCommand(InteractiveService interactiveService, ServerCommand serverCommand, SettingsCommand settingsCommand, UserSettingService userSettingService)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("user", "Show info for a server user")]
    [GuildOnly]
    public async Task UserServerCommand([Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show server user info for you")] bool? hide = null)
    {
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var guildMember = Context.Guild.Users.Single(guildUser => guildUser.Id == user.Id).AsMaybe();
        if (guildMember.HasNoValue)
        {
            var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
            embed.Embed.WithTitle($"The user you inserted is not in this server")
                .WithColor(Color.Red);
            await Context.SendResponse(interactiveService, embed, true);
            return;
        }
        await Context.SendResponse(interactiveService, await serverCommand.UserServerCommand(guildMember.Value), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
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
