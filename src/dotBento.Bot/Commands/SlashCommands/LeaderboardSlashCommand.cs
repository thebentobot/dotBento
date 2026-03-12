using NetCord;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Domain.Enums.Leaderboard;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[SlashCommand("leaderboard", "View leaderboards")]
public sealed class LeaderboardSlashCommand(
    InteractiveService interactiveService,
    LeaderboardCommand leaderboardCommand,
    UserSettingService userSettingService)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("server", "Server XP leaderboard")]
    public async Task ServerCommand(
        [SlashCommandParameter(Name = "hide", Description = "Only show the result for you")] bool? hide = null)
    {
        var guild = Context.Guild;
        var guildIconUrl = guild?.IconHash != null
            ? $"https://cdn.discordapp.com/icons/{guild.Id}/{guild.IconHash}.png"
            : null;
        await Context.SendResponse(interactiveService,
            await leaderboardCommand.GetServerXpLeaderboardAsync(
                (long)guild!.Id, guild.Name, guildIconUrl),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [SubSlashCommand("global", "Global XP leaderboard")]
    public async Task GlobalCommand(
        [SlashCommandParameter(Name = "hide", Description = "Only show the result for you")] bool? hide = null) =>
        await Context.SendResponse(interactiveService,
            await leaderboardCommand.GetGlobalXpLeaderboardAsync(
                Context.Client.Cache.User?.GetAvatarUrl()?.ToString(1024)),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));

    [SubSlashCommand("user", "View a user's ranking summary")]
    public async Task UserCommand(
        [SlashCommandParameter(Name = "user", Description = "Pick a user")] User? user = null,
        [SlashCommandParameter(Name = "hide", Description = "Only show the result for you")] bool? hide = null)
    {
        user ??= Context.User;
        var guild = Context.Guild;
        var guildUser = guild?.Users.GetValueOrDefault(user.Id);
        var displayName = guildUser?.Nickname ?? guildUser?.GlobalName ?? user.GlobalName ?? user.Username;
        var avatarUrl = guildUser?.GetGuildAvatarUrl()?.ToString(1024) ?? user.GetAvatarUrl()?.ToString(1024);
        await Context.SendResponse(interactiveService,
            await leaderboardCommand.GetUserSummaryAsync(
                (long)user.Id, (long)guild!.Id, displayName, avatarUrl, guild.Name),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [SubSlashCommand("bento", "Bento leaderboards")]
    public sealed class BentoGroup(
        InteractiveService interactiveService,
        LeaderboardCommand leaderboardCommand,
        UserSettingService userSettingService)
        : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SubSlashCommand("server", "Server bento leaderboard")]
        public async Task ServerCommand(
            [SlashCommandParameter(Name = "hide", Description = "Only show the result for you")] bool? hide = null)
        {
            var guild = Context.Guild;
            var guildIconUrl = guild?.IconHash != null
                ? $"https://cdn.discordapp.com/icons/{guild.Id}/{guild.IconHash}.png"
                : null;
            await Context.SendResponse(interactiveService,
                await leaderboardCommand.GetServerBentoLeaderboardAsync(
                    (long)guild!.Id, guild.Name, guildIconUrl),
                hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        }

        [SubSlashCommand("global", "Global bento leaderboard")]
        public async Task GlobalCommand(
            [SlashCommandParameter(Name = "hide", Description = "Only show the result for you")] bool? hide = null) =>
            await Context.SendResponse(interactiveService,
                await leaderboardCommand.GetGlobalBentoLeaderboardAsync(
                    Context.Client.Cache.User?.GetAvatarUrl()?.ToString(1024)),
                hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [SubSlashCommand("rps", "RPS leaderboards")]
    public sealed class RpsGroup(
        InteractiveService interactiveService,
        LeaderboardCommand leaderboardCommand,
        UserSettingService userSettingService)
        : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SubSlashCommand("server", "Server RPS leaderboard")]
        public async Task ServerCommand(
            [SlashCommandParameter(Name = "type", Description = "RPS weapon type filter")] RpsLeaderboardType? type = null,
            [SlashCommandParameter(Name = "order", Description = "Order by wins, ties, or losses")] RpsLeaderboardOrder? order = null,
            [SlashCommandParameter(Name = "hide", Description = "Only show the result for you")] bool? hide = null)
        {
            var guild = Context.Guild;
            var guildIconUrl = guild?.IconHash != null
                ? $"https://cdn.discordapp.com/icons/{guild.Id}/{guild.IconHash}.png"
                : null;
            await Context.SendResponse(interactiveService,
                await leaderboardCommand.GetServerRpsLeaderboardAsync(
                    (long)guild!.Id, guild.Name, guildIconUrl,
                    type ?? RpsLeaderboardType.All,
                    order ?? RpsLeaderboardOrder.Wins),
                hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        }

        [SubSlashCommand("global", "Global RPS leaderboard")]
        public async Task GlobalCommand(
            [SlashCommandParameter(Name = "type", Description = "RPS weapon type filter")] RpsLeaderboardType? type = null,
            [SlashCommandParameter(Name = "order", Description = "Order by wins, ties, or losses")] RpsLeaderboardOrder? order = null,
            [SlashCommandParameter(Name = "hide", Description = "Only show the result for you")] bool? hide = null) =>
            await Context.SendResponse(interactiveService,
                await leaderboardCommand.GetGlobalRpsLeaderboardAsync(
                    type ?? RpsLeaderboardType.All,
                    order ?? RpsLeaderboardOrder.Wins,
                    Context.Client.Cache.User?.GetAvatarUrl()?.ToString(1024)),
                hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }
}
