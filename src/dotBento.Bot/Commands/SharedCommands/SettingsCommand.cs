using Discord;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
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
                    ? $"**Public** - Anyone can view the server leaderboard on the [website](https://bentobot.xyz/leaderboard{guildId})."
                    : $"**Private** - Only server members can view the leaderboard on the [website](https://bentobot.xyz/leaderboard{guildId}).");

        if (!string.IsNullOrEmpty(guildIconUrl))
            response.Embed.WithThumbnailUrl(guildIconUrl);

        var leaderboardButton = new ButtonBuilder()
            .WithLabel(setting.LeaderboardPublic ? "Make Private" : "Make Public")
            .WithCustomId("server-settings:leaderboard-public")
            .WithStyle(setting.LeaderboardPublic ? ButtonStyle.Danger : ButtonStyle.Success);

        response.Components = new ComponentBuilder()
            .WithButton(leaderboardButton);

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
}
