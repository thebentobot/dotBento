using NetCord;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Services;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[SlashCommand("server", "Commands for the Discord Server")]
public sealed class ServerSlashCommand(InteractiveService interactiveService, ServerCommand serverCommand, SettingsCommand settingsCommand, UserSettingService userSettingService, GuildMemberLookupService memberLookup)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("user", "Show info for a server user")]
    [GuildOnly]
    public async Task UserServerCommand([SlashCommandParameter(Name = "user", Description = "Pick a User")] User? user = null,
        [SlashCommandParameter(Name = "hide", Description = "Only show server user info for you")] bool? hide = null)
    {
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var guildMember = await memberLookup.GetOrFetchAsync(Context.Guild!.Id, user.Id, Context.Guild);
        if (guildMember is null)
        {
            var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
            embed.Embed.WithTitle("The user you inserted is not in this server")
                .WithColor(new Color(0xFF0000));
            await Context.SendResponse(interactiveService, embed, true);
            return;
        }
        await Context.SendResponse(interactiveService, await serverCommand.UserServerCommand(guildMember, Context.Guild!), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [SubSlashCommand("info", "Show info for the server")]
    [GuildOnly]
    public async Task ServerCommand([SlashCommandParameter(Name = "hide", Description = "Only show server info for you")] bool? hide = null) =>
        await Context.SendResponse(interactiveService, await serverCommand.ServerInfoCommand(Context.Guild),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));

    [SubSlashCommand("settings", "View and manage server settings")]
    [GuildOnly]
    public async Task SettingsCommand()
    {
        var guildUser = await memberLookup.GetOrFetchAsync(Context.Guild!.Id, Context.User.Id, Context.Guild);
        if (guildUser is null || !guildUser.HasGuildPermission(Context.Guild!, Permissions.ManageGuild))
        {
            var embed = new ResponseModel { ResponseType = ResponseType.Embed };
            embed.Embed.WithTitle("Missing Permissions")
                .WithDescription("You need the **Manage Server** permission to use this command.")
                .WithColor(new Color(0xFF0000));
            await Context.SendResponse(interactiveService, embed, true);
            return;
        }
        await Context.SendResponse(interactiveService,
            await settingsCommand.GetServerSettingsAsync(
                (long)Context.Guild!.Id,
                Context.Guild?.Name ?? "",
                Context.Guild?.IconHash != null ? $"https://cdn.discordapp.com/icons/{Context.Guild.Id}/{Context.Guild.IconHash}.png" : null),
            true);
    }
}
