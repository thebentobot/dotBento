namespace dotBento.Infrastructure.Models;

public sealed record LeaderboardEntry(
    int Rank,
    long UserId,
    int Level,
    int Xp,
    string? Username,
    string? Discriminator,
    string? AvatarUrl);

public sealed record BentoLeaderboardEntry(
    int Rank,
    long UserId,
    int BentoCount,
    string? Username,
    string? Discriminator,
    string? AvatarUrl);

public sealed record RpsLeaderboardEntry(
    int Rank,
    long UserId,
    int Wins,
    int Ties,
    int Losses,
    string? Username,
    string? Discriminator);

public sealed record UserLeaderboardSummary(
    int? ServerXpRank,
    int ServerXpLevel,
    int ServerXpXp,
    int? GlobalXpRank,
    int GlobalXpLevel,
    int GlobalXpXp,
    int? BentoRank,
    int BentoCount,
    int? ServerBentoRank);
