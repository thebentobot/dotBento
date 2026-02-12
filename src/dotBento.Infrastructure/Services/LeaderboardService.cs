using CSharpFunctionalExtensions;
using dotBento.Domain.Enums.Leaderboard;
using dotBento.EntityFramework.Context;
using dotBento.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace dotBento.Infrastructure.Services;

public sealed class LeaderboardService(IDbContextFactory<BotDbContext> contextFactory)
{
    public async Task<Result<List<LeaderboardEntry>>> GetServerXpLeaderboardAsync(long guildId, int limit = 50)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var members = await context.GuildMembers
            .Where(gm => gm.GuildId == guildId)
            .Include(gm => gm.User)
            .OrderByDescending(gm => gm.Level)
            .ThenByDescending(gm => gm.Xp)
            .Take(limit)
            .ToListAsync();

        var entries = members.Select((gm, i) => new LeaderboardEntry(
            i + 1,
            gm.UserId,
            gm.Level,
            gm.Xp,
            gm.User.Username,
            gm.User.Discriminator,
            gm.AvatarUrl ?? gm.User.AvatarUrl)).ToList();

        return Result.Success(entries);
    }

    public async Task<Result<List<LeaderboardEntry>>> GetGlobalXpLeaderboardAsync(int limit = 50)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var users = await context.Users
            .OrderByDescending(u => u.Level)
            .ThenByDescending(u => u.Xp)
            .Take(limit)
            .ToListAsync();

        var entries = users.Select((u, i) => new LeaderboardEntry(
            i + 1,
            u.UserId,
            u.Level,
            u.Xp,
            u.Username,
            u.Discriminator,
            u.AvatarUrl)).ToList();

        return Result.Success(entries);
    }

    public async Task<Result<List<BentoLeaderboardEntry>>> GetServerBentoLeaderboardAsync(long guildId, int limit = 50)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var members = await context.GuildMembers
            .Where(gm => gm.GuildId == guildId)
            .Include(gm => gm.User)
            .ThenInclude(u => u.Bento)
            .Where(gm => gm.User.Bento != null)
            .OrderByDescending(gm => gm.User.Bento!.Bento1)
            .Take(limit)
            .ToListAsync();

        var entries = members.Select((gm, i) => new BentoLeaderboardEntry(
            i + 1,
            gm.UserId,
            gm.User.Bento?.Bento1 ?? 0,
            gm.User.Username,
            gm.User.Discriminator,
            gm.AvatarUrl ?? gm.User.AvatarUrl)).ToList();

        return Result.Success(entries);
    }

    public async Task<Result<List<BentoLeaderboardEntry>>> GetGlobalBentoLeaderboardAsync(int limit = 50)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var bentos = await context.Bentos
            .Include(b => b.User)
            .OrderByDescending(b => b.Bento1)
            .Take(limit)
            .ToListAsync();

        var entries = bentos.Select((b, i) => new BentoLeaderboardEntry(
            i + 1,
            b.UserId,
            b.Bento1,
            b.User.Username,
            b.User.Discriminator,
            b.User.AvatarUrl)).ToList();

        return Result.Success(entries);
    }

    public async Task<Result<List<RpsLeaderboardEntry>>> GetServerRpsLeaderboardAsync(
        long guildId, RpsLeaderboardType type, RpsLeaderboardOrder order, int limit = 50)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var guildUserIds = await context.GuildMembers
            .Where(gm => gm.GuildId == guildId)
            .Select(gm => gm.UserId)
            .ToListAsync();

        var query = context.RpsGames
            .Include(r => r.User)
            .Where(r => guildUserIds.Contains(r.UserId));

        var games = await query.ToListAsync();
        var entries = RankRpsGames(games, type, order, limit);

        return Result.Success(entries);
    }

    public async Task<Result<List<RpsLeaderboardEntry>>> GetGlobalRpsLeaderboardAsync(
        RpsLeaderboardType type, RpsLeaderboardOrder order, int limit = 50)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var games = await context.RpsGames
            .Include(r => r.User)
            .ToListAsync();

        var entries = RankRpsGames(games, type, order, limit);

        return Result.Success(entries);
    }

    public async Task<Result<UserLeaderboardSummary>> GetUserLeaderboardSummaryAsync(long userId, long guildId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var user = await context.Users
            .Include(u => u.Bento)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user is null)
            return Result.Failure<UserLeaderboardSummary>("User not found");

        // Global XP rank
        var globalXpRank = await context.Users
            .CountAsync(u => u.Level > user.Level || (u.Level == user.Level && u.Xp > user.Xp)) + 1;

        // Server XP rank
        var guildMember = await context.GuildMembers
            .FirstOrDefaultAsync(gm => gm.UserId == userId && gm.GuildId == guildId);

        int? serverXpRank = null;
        var serverXpLevel = 0;
        var serverXpXp = 0;
        if (guildMember is not null)
        {
            serverXpRank = await context.GuildMembers
                .Where(gm => gm.GuildId == guildId)
                .CountAsync(gm => gm.Level > guildMember.Level ||
                                  (gm.Level == guildMember.Level && gm.Xp > guildMember.Xp)) + 1;
            serverXpLevel = guildMember.Level;
            serverXpXp = guildMember.Xp;
        }

        // Bento rank
        int? bentoRank = null;
        var bentoCount = 0;
        if (user.Bento is not null)
        {
            bentoCount = user.Bento.Bento1;
            bentoRank = await context.Bentos
                .CountAsync(b => b.Bento1 > user.Bento.Bento1) + 1;
        }

        // Server bento rank
        int? serverBentoRank = null;
        if (user.Bento is not null && guildMember is not null)
        {
            var guildUserIds = await context.GuildMembers
                .Where(gm => gm.GuildId == guildId)
                .Select(gm => gm.UserId)
                .ToListAsync();

            serverBentoRank = await context.Bentos
                .Where(b => guildUserIds.Contains(b.UserId))
                .CountAsync(b => b.Bento1 > user.Bento.Bento1) + 1;
        }

        return Result.Success(new UserLeaderboardSummary(
            serverXpRank,
            serverXpLevel,
            serverXpXp,
            globalXpRank,
            user.Level,
            user.Xp,
            bentoRank,
            bentoCount,
            serverBentoRank));
    }

    private static List<RpsLeaderboardEntry> RankRpsGames(
        List<EntityFramework.Entities.RpsGame> games,
        RpsLeaderboardType type,
        RpsLeaderboardOrder order,
        int limit)
    {
        var projected = games.Select(g =>
        {
            var (wins, ties, losses) = type switch
            {
                RpsLeaderboardType.Rock => (
                    g.RockWins ?? 0,
                    g.RockTies ?? 0,
                    g.RockLosses ?? 0),
                RpsLeaderboardType.Paper => (
                    g.PaperWins ?? 0,
                    g.PaperTies ?? 0,
                    g.PaperLosses ?? 0),
                RpsLeaderboardType.Scissors => (
                    g.ScissorWins ?? 0,
                    g.ScissorsTies ?? 0,
                    g.ScissorsLosses ?? 0),
                _ => (
                    (g.RockWins ?? 0) + (g.PaperWins ?? 0) + (g.ScissorWins ?? 0),
                    (g.RockTies ?? 0) + (g.PaperTies ?? 0) + (g.ScissorsTies ?? 0),
                    (g.RockLosses ?? 0) + (g.PaperLosses ?? 0) + (g.ScissorsLosses ?? 0))
            };

            return new { g.UserId, Wins = wins, Ties = ties, Losses = losses, g.User.Username, g.User.Discriminator };
        });

        var ordered = order switch
        {
            RpsLeaderboardOrder.Wins => projected.OrderByDescending(x => x.Wins),
            RpsLeaderboardOrder.Ties => projected.OrderByDescending(x => x.Ties),
            RpsLeaderboardOrder.Losses => projected.OrderByDescending(x => x.Losses),
            _ => projected.OrderByDescending(x => x.Wins)
        };

        return ordered
            .Take(limit)
            .Select((x, i) => new RpsLeaderboardEntry(
                i + 1,
                x.UserId,
                x.Wins,
                x.Ties,
                x.Losses,
                x.Username,
                x.Discriminator))
            .ToList();
    }
}
