using Discord;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Bot.Utilities;
using dotBento.Infrastructure.Services;

namespace dotBento.Bot.Commands.SharedCommands;

public sealed class SettingsCommand(GuildSettingService guildSettingService, UserSettingService userSettingService)
{
    public async Task<ResponseModel> GetServerSettingsAsync(long guildId, string guildName, string? guildIconUrl)
    {
        var setting = await guildSettingService.GetOrCreateGuildSettingAsync(guildId);

        var response = new ResponseModel { ResponseType = ResponseType.Embed };

        response.Embed
            .WithTitle($"Server Settings for {guildName}")
            .WithColor(DiscordConstants.BentoYellow)
            .AddField("Website Leaderboard",
                setting.LeaderboardPublic
                    ? $"**Public** - Anyone can view the server leaderboard on the [website](https://bentobot.xyz/leaderboard/{guildId})."
                    : $"**Private** - Only server members can view the leaderboard on the [website](https://bentobot.xyz/leaderboard/{guildId}).");

        if (!string.IsNullOrEmpty(guildIconUrl))
            response.Embed.WithThumbnailUrl(guildIconUrl);

        var leaderboardButton = new ButtonBuilder()
            .WithLabel(setting.LeaderboardPublic ? "Make Private" : "Make Public")
            .WithCustomId("server-settings:leaderboard-public")
            .WithStyle(setting.LeaderboardPublic ? ButtonStyle.Danger : ButtonStyle.Success);

        var commandPermissionsButton = new ButtonBuilder()
            .WithLabel("Command Permissions")
            .WithCustomId("server-settings:commands:overview")
            .WithStyle(ButtonStyle.Primary);

        response.Components = new ComponentBuilder()
            .WithButton(leaderboardButton)
            .WithButton(commandPermissionsButton);

        return response;
    }

    public async Task<ResponseModel> GetUserSettingsAsync(long userId)
    {
        var setting = await userSettingService.GetOrCreateUserSettingAsync(userId);

        var response = new ResponseModel { ResponseType = ResponseType.Embed };

        response.Embed
            .WithTitle("Your Settings")
            .WithColor(DiscordConstants.BentoYellow)
            .AddField("Hide Slash Command responses",
                setting.HideSlashCommandCalls
                    ? "**Enabled ✔** - Your command responses are ephemeral (only visible to you) by default. When using a command, you can still choose the option to show the response to everyone."
                    : "**Disabled ⨯** - Your command responses are visible to everyone by default. When using a command, you can choose to hide the response.")
            .AddField("Global Leaderboard",
                setting.ShowOnGlobalLeaderboard
                    ? "**Enabled ✔** - You appear on the global leaderboard, which is the command and [website](https://bentobot.xyz/leaderboard)"
                    : "**Disabled ⨯** - You are hidden from the global leaderboard, so your user is shown as private in the command and [website](https://bentobot.xyz/leaderboard)");

        var hideCommandsButton = new ButtonBuilder()
            .WithLabel(setting.HideSlashCommandCalls ? "Show Commands" : "Hide Commands")
            .WithCustomId("user-settings:hide-commands")
            .WithStyle(setting.HideSlashCommandCalls ? ButtonStyle.Success : ButtonStyle.Secondary);

        var globalLeaderboardButton = new ButtonBuilder()
            .WithLabel(setting.ShowOnGlobalLeaderboard ? "Hide from Leaderboard" : "Show on Leaderboard")
            .WithCustomId("user-settings:global-leaderboard")
            .WithStyle(setting.ShowOnGlobalLeaderboard ? ButtonStyle.Danger : ButtonStyle.Success);

        response.Components = new ComponentBuilder()
            .WithButton(hideCommandsButton)
            .WithButton(globalLeaderboardButton);

        return response;
    }

    public async Task<ResponseModel> GetCommandSettingsViewAsync(
        long guildId,
        string guildName,
        string? guildIconUrl,
        IReadOnlyCollection<string> registeredCommandIds)
    {
        var permissions = await guildSettingService.GetCommandPermissionsAsync(guildId);

        var response = new ResponseModel { ResponseType = ResponseType.Embed };

        response.Embed
            .WithTitle($"Command Permissions for {guildName}")
            .WithDescription(
                "Choose how slash commands behave in this server. Disabled and admin-only are mutually exclusive.")
            .WithColor(DiscordConstants.BentoYellow)
            .AddField(
                $"Disabled Commands ({permissions.Disabled.Length})",
                BuildCommandListPreview(permissions.Disabled))
            .AddField(
                $"Admin-Only Commands ({permissions.AdminOnly.Length})",
                BuildCommandListPreview(permissions.AdminOnly));

        if (!string.IsNullOrEmpty(guildIconUrl))
            response.Embed.WithThumbnailUrl(guildIconUrl);

        var components = new ComponentBuilder();

        foreach (var action in Enum.GetValues<CommandPermissionAction>())
        {
            var candidates = GetCommandActionCandidates(action, permissions, registeredCommandIds);
            components.WithButton(new ButtonBuilder()
                .WithLabel(action.ToLabel())
                .WithCustomId($"server-settings:commands:page:{action.ToToken()}:0")
                .WithStyle(ActionButtonStyle(action))
                .WithDisabled(candidates.Count == 0), 0);
        }

        components.WithButton(new ButtonBuilder()
            .WithLabel("Back to Server Settings")
            .WithCustomId("server-settings:root")
            .WithStyle(ButtonStyle.Secondary), 1);

        response.Components = components;

        return response;
    }

    public async Task<ResponseModel> GetCommandActionViewAsync(
        long guildId,
        string guildName,
        string? guildIconUrl,
        CommandPermissionAction action,
        int requestedPage,
        IReadOnlyCollection<string> registeredCommandIds,
        string? notice = null)
    {
        const int pageSize = 25;

        var permissions = await guildSettingService.GetCommandPermissionsAsync(guildId);
        var candidates = GetCommandActionCandidates(action, permissions, registeredCommandIds);
        var pageCount = Math.Max(1, (int)Math.Ceiling(candidates.Count / (double)pageSize));
        var page = Math.Clamp(requestedPage, 0, pageCount - 1);
        var pageCandidates = candidates.Skip(page * pageSize).Take(pageSize).ToArray();

        var description = ActionDescription(action);
        if (!string.IsNullOrWhiteSpace(notice))
            description = $"{notice}\n\n{description}";

        var response = new ResponseModel { ResponseType = ResponseType.Embed };
        response.Embed
            .WithTitle($"{action.ToLabel()} Commands for {guildName}")
            .WithDescription(description)
            .WithColor(DiscordConstants.BentoYellow)
            .WithFooter($"Page {page + 1} of {pageCount} • {candidates.Count} commands available");

        if (!string.IsNullOrEmpty(guildIconUrl))
            response.Embed.WithThumbnailUrl(guildIconUrl);

        var components = new ComponentBuilder();

        if (pageCandidates.Length > 0)
        {
            var menu = new SelectMenuBuilder()
                .WithCustomId($"server-settings:commands:select:{action.ToToken()}:{page}")
                .WithPlaceholder("Select one command")
                .WithMinValues(1)
                .WithMaxValues(1);

            foreach (var commandId in pageCandidates)
                menu.AddOption(commandId, commandId);

            components.WithSelectMenu(menu, 0);
            components
                .WithButton("Previous", $"server-settings:commands:page:{action.ToToken()}:{page - 1}",
                    ButtonStyle.Secondary, disabled: page == 0, row: 1)
                .WithButton("Back to Overview", "server-settings:commands:overview",
                    ButtonStyle.Secondary, row: 1)
                .WithButton("Next", $"server-settings:commands:page:{action.ToToken()}:{page + 1}",
                    ButtonStyle.Secondary, disabled: page >= pageCount - 1, row: 1);
        }
        else
        {
            response.Embed.AddField("Nothing to change", EmptyActionDescription(action));
            components.WithButton("Back to Overview", "server-settings:commands:overview",
                ButtonStyle.Secondary);
        }

        response.Components = components;
        return response;
    }

    public async Task<string> ApplyCommandPermissionActionAsync(
        long guildId,
        string commandId,
        CommandPermissionAction action)
    {
        var permissions = await guildSettingService.GetCommandPermissionsAsync(guildId);

        switch (action)
        {
            case CommandPermissionAction.Disable:
                if (permissions.Disabled.Contains(commandId, StringComparer.OrdinalIgnoreCase))
                    return $"`{commandId}` is already disabled.";
                await guildSettingService.SetCommandDisabledAsync(guildId, commandId, true);
                return $"`{commandId}` is now **disabled**.";

            case CommandPermissionAction.Enable:
                if (!permissions.Disabled.Contains(commandId, StringComparer.OrdinalIgnoreCase))
                    return $"`{commandId}` is not currently disabled.";
                await guildSettingService.SetCommandDisabledAsync(guildId, commandId, false);
                return $"`{commandId}` is now **enabled**.";

            case CommandPermissionAction.AddAdminOnly:
                if (permissions.AdminOnly.Contains(commandId, StringComparer.OrdinalIgnoreCase))
                    return $"`{commandId}` is already admin-only.";
                await guildSettingService.SetCommandAdminOnlyAsync(guildId, commandId, true);
                return $"`{commandId}` is now **admin-only**.";

            case CommandPermissionAction.RemoveAdminOnly:
                if (!permissions.AdminOnly.Contains(commandId, StringComparer.OrdinalIgnoreCase))
                    return $"`{commandId}` is not currently admin-only.";
                await guildSettingService.SetCommandAdminOnlyAsync(guildId, commandId, false);
                return $"`{commandId}` is no longer **admin-only**.";

            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    public async Task<ResponseModel> ToggleLeaderboardPublicAsync(long guildId, string guildName, string? guildIconUrl)
    {
        var current = await guildSettingService.GetOrCreateGuildSettingAsync(guildId);
        await guildSettingService.UpdateLeaderboardPublicAsync(guildId, !current.LeaderboardPublic);
        return await GetServerSettingsAsync(guildId, guildName, guildIconUrl);
    }

    public async Task<ResponseModel> ToggleHideCommandsAsync(long userId)
    {
        var current = await userSettingService.GetOrCreateUserSettingAsync(userId);
        await userSettingService.UpdateUserSettingAsync(userId, s => s.HideSlashCommandCalls = !current.HideSlashCommandCalls);
        return await GetUserSettingsAsync(userId);
    }

    public async Task<ResponseModel> ToggleGlobalLeaderboardAsync(long userId)
    {
        var current = await userSettingService.GetOrCreateUserSettingAsync(userId);
        await userSettingService.UpdateUserSettingAsync(userId, s => s.ShowOnGlobalLeaderboard = !current.ShowOnGlobalLeaderboard);
        return await GetUserSettingsAsync(userId);
    }

    public static IReadOnlyList<string> GetCommandActionCandidates(
        CommandPermissionAction action,
        CommandPermissions permissions,
        IReadOnlyCollection<string> registeredCommandIds)
    {
        var disabled = new HashSet<string>(permissions.Disabled, StringComparer.OrdinalIgnoreCase);
        var adminOnly = new HashSet<string>(permissions.AdminOnly, StringComparer.OrdinalIgnoreCase);
        var manageable = registeredCommandIds
            .Where(id => !SlashCommandIdUtilities.IsProtectedCommand(id))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var candidates = action switch
        {
            CommandPermissionAction.Disable => manageable.Where(id => !disabled.Contains(id)),
            CommandPermissionAction.Enable => disabled,
            CommandPermissionAction.AddAdminOnly => manageable.Where(id => !adminOnly.Contains(id)),
            CommandPermissionAction.RemoveAdminOnly => adminOnly,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

        return candidates
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string BuildCommandListPreview(IEnumerable<string> commands)
    {
        const int previewCharacterLimit = 900;
        var sorted = commands
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (sorted.Length == 0)
            return "None";

        var lines = new List<string>();
        foreach (var command in sorted)
        {
            var line = $"• `{command}`";
            var candidateLength = lines.Sum(existing => existing.Length + 1) + line.Length;
            if (candidateLength > previewCharacterLimit)
                break;
            lines.Add(line);
        }

        var omitted = sorted.Length - lines.Count;
        if (omitted > 0)
            lines.Add($"…and {omitted} more. Use the controls below to review them.");

        return string.Join("\n", lines);
    }

    private static ButtonStyle ActionButtonStyle(CommandPermissionAction action) =>
        action switch
        {
            CommandPermissionAction.Disable => ButtonStyle.Danger,
            CommandPermissionAction.Enable => ButtonStyle.Success,
            CommandPermissionAction.AddAdminOnly => ButtonStyle.Primary,
            CommandPermissionAction.RemoveAdminOnly => ButtonStyle.Secondary,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

    private static string ActionDescription(CommandPermissionAction action) =>
        action switch
        {
            CommandPermissionAction.Disable =>
                "Select an enabled command to disable. If it is admin-only, disabling it replaces that restriction.",
            CommandPermissionAction.Enable =>
                "Select a disabled command to restore its default permissions.",
            CommandPermissionAction.AddAdminOnly =>
                "Select a command to require the **Manage Server** permission. This replaces a disabled restriction.",
            CommandPermissionAction.RemoveAdminOnly =>
                "Select an admin-only command to restore its default permissions.",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

    private static string EmptyActionDescription(CommandPermissionAction action) =>
        action switch
        {
            CommandPermissionAction.Disable => "Every manageable command is already disabled.",
            CommandPermissionAction.Enable => "There are no disabled commands.",
            CommandPermissionAction.AddAdminOnly => "Every manageable command is already admin-only.",
            CommandPermissionAction.RemoveAdminOnly => "There are no admin-only commands.",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
}
