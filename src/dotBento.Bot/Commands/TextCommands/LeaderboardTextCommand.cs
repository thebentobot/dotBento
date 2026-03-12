using NetCord;
using NetCord.Services.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Domain.Enums.Leaderboard;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[ModuleName("Leaderboard")]
public sealed class LeaderboardTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    LeaderboardCommand leaderboardCommand) : BaseCommandModule(botSettings)
{
    [Command("leaderboard", "lb", "ranking", "rankings")]
    [Summary("View leaderboards for XP, bento, and RPS")]
    [Examples(
        "leaderboard",
        "leaderboard global",
        "leaderboard bento",
        "leaderboard bento global",
        "leaderboard rps",
        "leaderboard rps global",
        "leaderboard rps rock wins",
        "leaderboard rps global paper losses",
        "leaderboard user @someone")]
    [GuildOnly]
    public async Task LeaderboardCommand([CommandParameter(Remainder = true)] string? input = null)
    {
        _ = Context.Channel?.TriggerTypingStateAsync();

        var guildId = (long)Context.Guild!.Id;
        var guildName = Context.Guild.Name;
        var guildIconUrl = Context.Guild.IconHash != null ? $"https://cdn.discordapp.com/icons/{Context.Guild.Id}/{Context.Guild.IconHash}.png" : null;
        var botAvatarUrl = Context.Client.Cache.User?.GetAvatarUrl()?.ToString(1024) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            await Context.SendResponse(interactiveService,
                await leaderboardCommand.GetServerXpLeaderboardAsync(guildId, guildName, guildIconUrl));
            return;
        }

        var args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = args[0].ToLower();

        switch (command)
        {
            case "global":
                await Context.SendResponse(interactiveService,
                    await leaderboardCommand.GetGlobalXpLeaderboardAsync(botAvatarUrl));
                break;

            case "bento":
                if (args.Length > 1 && args[1].Equals("global", StringComparison.OrdinalIgnoreCase))
                {
                    await Context.SendResponse(interactiveService,
                        await leaderboardCommand.GetGlobalBentoLeaderboardAsync(botAvatarUrl));
                }
                else
                {
                    await Context.SendResponse(interactiveService,
                        await leaderboardCommand.GetServerBentoLeaderboardAsync(guildId, guildName, guildIconUrl));
                }
                break;

            case "rps":
                await HandleRpsCommand(args, guildId, guildName, guildIconUrl, botAvatarUrl);
                break;

            case "user":
                await HandleUserCommand(args);
                break;

            default:
                await Context.SendResponse(interactiveService, ErrorEmbed(
                    "Invalid subcommand. Use: `leaderboard [global|bento|bento global|rps|rps global|user @someone]`"));
                break;
        }
    }

    private async Task HandleRpsCommand(string[] args, long guildId, string guildName, string? guildIconUrl, string botAvatarUrl)
    {
        var isGlobal = args.Length > 1 && args[1].Equals("global", StringComparison.OrdinalIgnoreCase);
        var typeArgs = isGlobal ? args.Skip(2).ToArray() : args.Skip(1).ToArray();

        var type = RpsLeaderboardType.All;
        var order = RpsLeaderboardOrder.Wins;

        if (typeArgs.Length > 0 && Enum.TryParse<RpsLeaderboardType>(typeArgs[0], true, out var parsedType))
        {
            type = parsedType;
        }

        if (typeArgs.Length > 1 && Enum.TryParse<RpsLeaderboardOrder>(typeArgs[1], true, out var parsedOrder))
        {
            order = parsedOrder;
        }

        if (isGlobal)
        {
            await Context.SendResponse(interactiveService,
                await leaderboardCommand.GetGlobalRpsLeaderboardAsync(type, order, botAvatarUrl));
        }
        else
        {
            await Context.SendResponse(interactiveService,
                await leaderboardCommand.GetServerRpsLeaderboardAsync(guildId, guildName, guildIconUrl, type, order));
        }
    }

    private async Task HandleUserCommand(string[] args)
    {
        User user;
        if (args.Length > 1)
        {
            var mentionedUser = Context.Message.MentionedUsers.FirstOrDefault();
            if (mentionedUser != null)
            {
                user = mentionedUser;
            }
            else if (ulong.TryParse(args[1], out var userId))
            {
                var resolvedUser = Context.Guild?.Users.GetValueOrDefault(userId);
                if (resolvedUser == null)
                {
                    await Context.SendResponse(interactiveService, ErrorEmbed("User not found."));
                    return;
                }
                user = resolvedUser;
            }
            else
            {
                await Context.SendResponse(interactiveService, ErrorEmbed("Invalid user. Mention a user or provide a user ID."));
                return;
            }
        }
        else
        {
            user = Context.User;
        }

        var guild = Context.Guild!;
        var guildUser = guild.Users.GetValueOrDefault(user.Id);
        var displayName = guildUser?.Nickname ?? user.GlobalName ?? user.Username;
        var avatarUrl = guildUser?.GetGuildAvatarUrl()?.ToString(1024) ?? user.GetAvatarUrl()?.ToString(1024) ?? user.DefaultAvatarUrl.ToString(1024);

        await Context.SendResponse(interactiveService,
            await leaderboardCommand.GetUserSummaryAsync(
                (long)user.Id, (long)guild.Id, displayName, avatarUrl, guild.Name));
    }

    private static ResponseModel ErrorEmbed(string error)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle(error)
            .WithColor(new Color(0xFF0000));
        return embed;
    }
}
