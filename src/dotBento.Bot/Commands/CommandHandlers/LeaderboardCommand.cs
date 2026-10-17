using System.Text;
using dotBento.Domain;
using Discord;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Bot.Services;
using dotBento.Domain.Enums.Leaderboard;
using dotBento.Infrastructure.Models;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.CommandHandlers;

public sealed class LeaderboardCommand(LeaderboardService leaderboardService, UserSettingService userSettingService, BotPresentation? presentation = null)
{
    private readonly BotPresentation _presentation = presentation ?? new();

    public async Task<ResponseModel> GetServerXpLeaderboardAsync(long guildId, string guildName, string? guildIconUrl)
    {
        var result = await leaderboardService.GetServerXpLeaderboardAsync(guildId);
        if (result.IsFailure)
            return _presentation.ErrorEmbed(result.Error);

        if (result.Value.Count == 0)
            return _presentation.ErrorEmbed("No users found on the server leaderboard.");

        var leaderboardUrl = Branding.JoinUrl(_presentation.Brand.WebsiteUrl, $"leaderboard/{guildId}");
        return BuildXpPaginator(result.Value, $"Leaderboard for {guildName}", guildIconUrl, leaderboardUrl);
    }

    public async Task<ResponseModel> GetGlobalXpLeaderboardAsync(string? botAvatarUrl)
    {
        var result = await leaderboardService.GetGlobalXpLeaderboardAsync();
        if (result.IsFailure)
            return _presentation.ErrorEmbed(result.Error);

        if (result.Value.Count == 0)
            return _presentation.ErrorEmbed("No users found on the global leaderboard.");

        var hiddenUserIds = await userSettingService.GetHiddenGlobalLeaderboardUserIdsAsync();
        var anonymized = result.Value.Select(e => hiddenUserIds.Contains(e.UserId)
            ? e with { Username = "Private" }
            : e).ToList();

        var leaderboardUrl = Branding.JoinUrl(_presentation.Brand.WebsiteUrl, "leaderboard");
        return BuildXpPaginator(anonymized, "Global Leaderboard", botAvatarUrl, leaderboardUrl);
    }

    public async Task<ResponseModel> GetServerBentoLeaderboardAsync(long guildId, string guildName, string? guildIconUrl)
    {
        var result = await leaderboardService.GetServerBentoLeaderboardAsync(guildId);
        if (result.IsFailure)
            return _presentation.ErrorEmbed(result.Error);

        if (result.Value.Count == 0)
            return _presentation.ErrorEmbed($"No users found on the server {_presentation.Brand.GiftName} leaderboard.");

        var leaderboardUrl = Branding.JoinUrl(_presentation.Brand.WebsiteUrl, $"leaderboard/{guildId}");
        return BuildBentoPaginator(result.Value, $"{_presentation.Brand.GiftName} Leaderboard for {guildName}", guildIconUrl, leaderboardUrl);
    }

    public async Task<ResponseModel> GetGlobalBentoLeaderboardAsync(string? botAvatarUrl)
    {
        var result = await leaderboardService.GetGlobalBentoLeaderboardAsync();
        if (result.IsFailure)
            return _presentation.ErrorEmbed(result.Error);

        if (result.Value.Count == 0)
            return _presentation.ErrorEmbed($"No users found on the global {_presentation.Brand.GiftName} leaderboard.");

        var hiddenUserIds = await userSettingService.GetHiddenGlobalLeaderboardUserIdsAsync();
        var anonymized = result.Value.Select(e => hiddenUserIds.Contains(e.UserId)
            ? e with { Username = "Private" }
            : e).ToList();

        var leaderboardUrl = Branding.JoinUrl(_presentation.Brand.WebsiteUrl, "leaderboard");
        return BuildBentoPaginator(anonymized, $"Global {_presentation.Brand.GiftName} Leaderboard", botAvatarUrl, leaderboardUrl);
    }

    public async Task<ResponseModel> GetServerRpsLeaderboardAsync(
        long guildId, string guildName, string? guildIconUrl,
        RpsLeaderboardType type, RpsLeaderboardOrder order)
    {
        var result = await leaderboardService.GetServerRpsLeaderboardAsync(guildId, type, order);
        if (result.IsFailure)
            return _presentation.ErrorEmbed(result.Error);

        if (result.Value.Count == 0)
            return _presentation.ErrorEmbed("No users found on the server RPS leaderboard.");

        var typeLabel = type == RpsLeaderboardType.All ? "" : $" ({type})";
        var leaderboardUrl = Branding.JoinUrl(_presentation.Brand.WebsiteUrl, $"leaderboard/{guildId}");
        return BuildRpsPaginator(result.Value, $"RPS Leaderboard for {guildName}{typeLabel}", guildIconUrl, order, leaderboardUrl);
    }

    public async Task<ResponseModel> GetGlobalRpsLeaderboardAsync(
        RpsLeaderboardType type, RpsLeaderboardOrder order, string? botAvatarUrl)
    {
        var result = await leaderboardService.GetGlobalRpsLeaderboardAsync(type, order);
        if (result.IsFailure)
            return _presentation.ErrorEmbed(result.Error);

        if (result.Value.Count == 0)
            return _presentation.ErrorEmbed("No users found on the global RPS leaderboard.");

        var hiddenUserIds = await userSettingService.GetHiddenGlobalLeaderboardUserIdsAsync();
        var anonymized = result.Value.Select(e => hiddenUserIds.Contains(e.UserId)
            ? e with { Username = "Private" }
            : e).ToList();

        var typeLabel = type == RpsLeaderboardType.All ? "" : $" ({type})";
        var leaderboardUrl = Branding.JoinUrl(_presentation.Brand.WebsiteUrl, "leaderboard");
        return BuildRpsPaginator(anonymized, $"Global RPS Leaderboard{typeLabel}", botAvatarUrl, order, leaderboardUrl);
    }

    public async Task<ResponseModel> GetUserSummaryAsync(
        long userId, long guildId, string displayName, string avatarUrl, string guildName)
    {
        var result = await leaderboardService.GetUserLeaderboardSummaryAsync(userId, guildId);
        if (result.IsFailure)
            return _presentation.ErrorEmbed(result.Error);

        var summary = result.Value;
        var embed = new ResponseModel { ResponseType = ResponseType.Embed };

        var description = new StringBuilder();

        if (summary.ServerXpRank.HasValue)
        {
            description.AppendLine($"**{guildName} Rank:** #{summary.ServerXpRank} — Level {summary.ServerXpLevel} ({summary.ServerXpXp} XP)");
        }

        description.AppendLine($"**Global Rank:** #{summary.GlobalXpRank} — Level {summary.GlobalXpLevel} ({summary.GlobalXpXp} XP)");

        if (summary.ServerBentoRank.HasValue)
        {
            description.AppendLine($"**{guildName} {_presentation.Brand.GiftName} Rank:** #{summary.ServerBentoRank}");
        }

        if (summary.BentoRank.HasValue)
        {
            description.AppendLine($"**Global {_presentation.Brand.GiftName} Rank:** #{summary.BentoRank} — {summary.BentoCount} {_presentation.Brand.Gift(summary.BentoCount)}");
        }

        var author = new EmbedAuthorBuilder
        {
            Name = $"{displayName}'s Rankings",
            IconUrl = avatarUrl
        };

        embed.Embed
            .WithAuthor(author)
            .WithThumbnailUrl(avatarUrl)
            .WithDescription(description.ToString())
            .WithColor(_presentation.Primary);

        return embed;
    }

    private ResponseModel BuildXpPaginator(List<LeaderboardEntry> entries, string title, string? iconUrl, string leaderboardUrl)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Paginator };
        var pageChunks = entries.ChunkBy(10);
        var pages = new List<PageBuilder>();

        foreach (var chunk in pageChunks)
        {
            var pageDescription = string.Join('\n', chunk.Select(entry =>
                $"**{entry.Rank}.** {entry.Username ?? "Unknown"} — Level {entry.Level} ({entry.Xp} XP)"));

            var page = new PageBuilder()
                .WithTitle(title)
                .WithDescription(pageDescription)
                .WithColor(_presentation.Primary);

            if (!string.IsNullOrWhiteSpace(leaderboardUrl)) page.WithUrl(leaderboardUrl);
            if (!string.IsNullOrEmpty(iconUrl))
                page.WithThumbnailUrl(iconUrl);

            pages.Add(page);
        }

        embed.StaticPaginator = pages.BuildSimpleStaticPaginator(_presentation);
        return embed;
    }

    private ResponseModel BuildBentoPaginator(List<BentoLeaderboardEntry> entries, string title, string? iconUrl, string leaderboardUrl)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Paginator };
        var pageChunks = entries.ChunkBy(10);
        var pages = new List<PageBuilder>();

        foreach (var chunk in pageChunks)
        {
            var pageDescription = string.Join('\n', chunk.Select(entry =>
                $"**{entry.Rank}.** {entry.Username ?? "Unknown"} — {entry.BentoCount} {_presentation.Brand.Gift(entry.BentoCount)} {_presentation.Brand.GiftEmoji}"));

            var page = new PageBuilder()
                .WithTitle(title)
                .WithDescription(pageDescription)
                .WithColor(_presentation.Primary);

            if (!string.IsNullOrWhiteSpace(leaderboardUrl)) page.WithUrl(leaderboardUrl);
            if (!string.IsNullOrEmpty(iconUrl))
                page.WithThumbnailUrl(iconUrl);

            pages.Add(page);
        }

        embed.StaticPaginator = pages.BuildSimpleStaticPaginator(_presentation);
        return embed;
    }

    private ResponseModel BuildRpsPaginator(List<RpsLeaderboardEntry> entries, string title, string? iconUrl, RpsLeaderboardOrder order, string leaderboardUrl)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Paginator };
        var pageChunks = entries.ChunkBy(10);
        var pages = new List<PageBuilder>();

        foreach (var chunk in pageChunks)
        {
            var pageDescription = string.Join('\n', chunk.Select(entry =>
            {
                var total = entry.Wins + entry.Ties + entry.Losses;
                var winRate = total > 0 ? (int)Math.Round((double)entry.Wins / total * 100) : 0;
                return $"**{entry.Rank}.** {entry.Username ?? "Unknown"} — {entry.Wins}W / {entry.Ties}T / {entry.Losses}L ({winRate}%)";
            }));

            var page = new PageBuilder()
                .WithTitle(title)
                .WithFooter($"Ordered by {order}")
                .WithDescription(pageDescription)
                .WithColor(_presentation.Primary);

            if (!string.IsNullOrWhiteSpace(leaderboardUrl)) page.WithUrl(leaderboardUrl);
            if (!string.IsNullOrEmpty(iconUrl))
                page.WithThumbnailUrl(iconUrl);

            pages.Add(page);
        }

        embed.StaticPaginator = pages.BuildSimpleStaticPaginator(_presentation);
        return embed;
    }

}
