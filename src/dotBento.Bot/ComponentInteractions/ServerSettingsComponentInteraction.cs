using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Utilities;
using dotBento.Infrastructure.Services;

namespace dotBento.Bot.ComponentInteractions;

public sealed class ServerSettingsComponentInteraction(
    SettingsCommand settingsCommand,
    GuildSettingService guildSettingService,
    InteractionService interactionService)
    : InteractionModuleBase<SocketInteractionContext>
{
    [ComponentInteraction("server-settings:leaderboard-public")]
    public async Task ToggleLeaderboardPublic()
    {
        if (!await EnsureCanManageServerAsync())
            return;

        var response = await settingsCommand.ToggleLeaderboardPublicAsync(
            (long)Context.Guild.Id, Context.Guild.Name, Context.Guild.IconUrl);
        await UpdateMessageAsync(response);
    }

    [ComponentInteraction("server-settings:root")]
    public async Task ShowServerSettings()
    {
        if (!await EnsureCanManageServerAsync())
            return;

        var response = await settingsCommand.GetServerSettingsAsync(
            (long)Context.Guild.Id, Context.Guild.Name, Context.Guild.IconUrl);
        await UpdateMessageAsync(response);
    }

    [ComponentInteraction("server-settings:commands:overview")]
    public async Task ShowCommandPermissions()
    {
        if (!await EnsureCanManageServerAsync())
            return;

        var response = await settingsCommand.GetCommandSettingsViewAsync(
            (long)Context.Guild.Id,
            Context.Guild.Name,
            Context.Guild.IconUrl,
            SlashCommandIdUtilities.GetManageableCommandIds(interactionService));
        await UpdateMessageAsync(response);
    }

    [ComponentInteraction("server-settings:commands:page:*:*")]
    public async Task ShowCommandAction(string actionToken, string pageToken)
    {
        if (!await EnsureCanManageServerAsync())
            return;

        if (!TryParseNavigation(actionToken, pageToken, out var action, out var page))
        {
            await RespondAsync("That command-settings page is invalid. Reopen `/server settings`.",
                ephemeral: true);
            return;
        }

        var response = await settingsCommand.GetCommandActionViewAsync(
            (long)Context.Guild.Id,
            Context.Guild.Name,
            Context.Guild.IconUrl,
            action,
            page,
            SlashCommandIdUtilities.GetManageableCommandIds(interactionService));
        await UpdateMessageAsync(response);
    }

    [ComponentInteraction("server-settings:commands:select:*:*")]
    public async Task UpdateCommandPermission(
        string actionToken,
        string pageToken,
        string[] selectedCommandIds)
    {
        if (!await EnsureCanManageServerAsync())
            return;

        if (!TryParseNavigation(actionToken, pageToken, out var action, out var page) ||
            selectedCommandIds.Length != 1)
        {
            await RespondAsync("That command-settings selection is invalid. Reopen `/server settings`.",
                ephemeral: true);
            return;
        }

        var commandId = selectedCommandIds[0];
        var registeredCommandIds = SlashCommandIdUtilities.GetManageableCommandIds(interactionService);
        var permissions = await guildSettingService.GetCommandPermissionsAsync((long)Context.Guild.Id);

        if (!CanApplySelection(action, commandId, registeredCommandIds, permissions))
        {
            var staleResponse = await settingsCommand.GetCommandActionViewAsync(
                (long)Context.Guild.Id,
                Context.Guild.Name,
                Context.Guild.IconUrl,
                action,
                page,
                registeredCommandIds,
                "That command is no longer available for this action. The list has been refreshed.");
            await UpdateMessageAsync(staleResponse);
            return;
        }

        var result = await settingsCommand.ApplyCommandPermissionActionAsync(
            (long)Context.Guild.Id, commandId, action);
        var response = await settingsCommand.GetCommandActionViewAsync(
            (long)Context.Guild.Id,
            Context.Guild.Name,
            Context.Guild.IconUrl,
            action,
            page,
            registeredCommandIds,
            result);
        await UpdateMessageAsync(response);
    }

    private async Task<bool> EnsureCanManageServerAsync()
    {
        if (Context.Guild is null)
        {
            await RespondAsync("Server settings are not available in DMs.", ephemeral: true);
            return false;
        }

        var guildUser = Context.Guild.GetUser(Context.User.Id);
        if (guildUser is null || !guildUser.GuildPermissions.ManageGuild)
        {
            await RespondAsync("You need the **Manage Server** permission to change server settings.",
                ephemeral: true);
            return false;
        }

        return true;
    }

    private static bool TryParseNavigation(
        string actionToken,
        string pageToken,
        out CommandPermissionAction action,
        out int page)
    {
        page = 0;
        return actionToken.TryParseCommandPermissionAction(out action) &&
               int.TryParse(pageToken, out page) &&
               page >= 0;
    }

    private static bool CanApplySelection(
        CommandPermissionAction action,
        string commandId,
        IReadOnlyCollection<string> registeredCommandIds,
        CommandPermissions permissions)
    {
        if (string.IsNullOrWhiteSpace(commandId))
            return false;

        return action switch
        {
            CommandPermissionAction.Disable or CommandPermissionAction.AddAdminOnly
                when !SlashCommandIdUtilities.IsProtectedCommand(commandId) =>
                registeredCommandIds.Contains(commandId, StringComparer.OrdinalIgnoreCase),
            CommandPermissionAction.Enable =>
                permissions.Disabled.Contains(commandId, StringComparer.OrdinalIgnoreCase),
            CommandPermissionAction.RemoveAdminOnly =>
                permissions.AdminOnly.Contains(commandId, StringComparer.OrdinalIgnoreCase),
            _ => false
        };
    }

    private async Task UpdateMessageAsync(ResponseModel response)
    {
        if (Context.Interaction is not SocketMessageComponent component)
        {
            await RespondAsync("This settings interaction is no longer available.", ephemeral: true);
            return;
        }

        await component.UpdateAsync(message =>
        {
            message.Embed = response.Embed.Build();
            message.Components = response.Components?.Build();
        });
    }
}
