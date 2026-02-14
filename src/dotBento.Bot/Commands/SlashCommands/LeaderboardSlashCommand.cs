using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Domain.Enums.Leaderboard;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("leaderboard", "View leaderboards")]
public sealed class LeaderboardSlashCommand(
    InteractiveService interactiveService,
    LeaderboardCommand leaderboardCommand,
    UserSettingService userSettingService)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("server", "Server XP leaderboard")]
    public async Task ServerCommand(
        [Summary("hide", "Only show the result for you")] bool? hide = null)
    {
        var guild = Context.Guild;
        await Context.SendResponse(interactiveService,
            await leaderboardCommand.GetServerXpLeaderboardAsync(
                (long)guild.Id, guild.Name, guild.IconUrl),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [SlashCommand("global", "Global XP leaderboard")]
    public async Task GlobalCommand(
        [Summary("hide", "Only show the result for you")] bool? hide = null) =>
        await Context.SendResponse(interactiveService,
            await leaderboardCommand.GetGlobalXpLeaderboardAsync(
                Context.Client.CurrentUser.GetDisplayAvatarUrl()),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));

    [SlashCommand("user", "View a user's ranking summary")]
    public async Task UserCommand(
        [Summary("user", "Pick a user")] SocketUser? user = null,
        [Summary("hide", "Only show the result for you")] bool? hide = null)
    {
        user ??= Context.User;
        var guild = Context.Guild;
        var displayName = guild.Users.FirstOrDefault(x => x.Id == user.Id)?.Nickname
                          ?? user.GlobalName ?? user.Username;
        var avatarUrl = guild.Users.FirstOrDefault(x => x.Id == user.Id)?.GetGuildAvatarUrl()
                        ?? user.GetDisplayAvatarUrl();
        await Context.SendResponse(interactiveService,
            await leaderboardCommand.GetUserSummaryAsync(
                (long)user.Id, (long)guild.Id, displayName, avatarUrl, guild.Name),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [Group("bento", "Bento leaderboards")]
    public sealed class BentoGroup(
        InteractiveService interactiveService,
        LeaderboardCommand leaderboardCommand,
        UserSettingService userSettingService)
        : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("server", "Server bento leaderboard")]
        public async Task ServerCommand(
            [Summary("hide", "Only show the result for you")] bool? hide = null)
        {
            var guild = Context.Guild;
            await Context.SendResponse(interactiveService,
                await leaderboardCommand.GetServerBentoLeaderboardAsync(
                    (long)guild.Id, guild.Name, guild.IconUrl),
                hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        }

        [SlashCommand("global", "Global bento leaderboard")]
        public async Task GlobalCommand(
            [Summary("hide", "Only show the result for you")] bool? hide = null) =>
            await Context.SendResponse(interactiveService,
                await leaderboardCommand.GetGlobalBentoLeaderboardAsync(
                    Context.Client.CurrentUser.GetDisplayAvatarUrl()),
                hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [Group("rps", "RPS leaderboards")]
    public sealed class RpsGroup(
        InteractiveService interactiveService,
        LeaderboardCommand leaderboardCommand,
        UserSettingService userSettingService)
        : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("server", "Server RPS leaderboard")]
        public async Task ServerCommand(
            [Summary("type", "RPS weapon type filter")] RpsLeaderboardType? type = RpsLeaderboardType.All,
            [Summary("order", "Order by wins, ties, or losses")] RpsLeaderboardOrder? order = RpsLeaderboardOrder.Wins,
            [Summary("hide", "Only show the result for you")] bool? hide = null)
        {
            var guild = Context.Guild;
            await Context.SendResponse(interactiveService,
                await leaderboardCommand.GetServerRpsLeaderboardAsync(
                    (long)guild.Id, guild.Name, guild.IconUrl,
                    type ?? RpsLeaderboardType.All,
                    order ?? RpsLeaderboardOrder.Wins),
                hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        }

        [SlashCommand("global", "Global RPS leaderboard")]
        public async Task GlobalCommand(
            [Summary("type", "RPS weapon type filter")] RpsLeaderboardType? type = RpsLeaderboardType.All,
            [Summary("order", "Order by wins, ties, or losses")] RpsLeaderboardOrder? order = RpsLeaderboardOrder.Wins,
            [Summary("hide", "Only show the result for you")] bool? hide = null) =>
            await Context.SendResponse(interactiveService,
                await leaderboardCommand.GetGlobalRpsLeaderboardAsync(
                    type ?? RpsLeaderboardType.All,
                    order ?? RpsLeaderboardOrder.Wins,
                    Context.Client.CurrentUser.GetDisplayAvatarUrl()),
                hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }
}
