using dotBento.EntityFramework.Entities;

namespace dotBento.Infrastructure.Commands.Profile;

/// <summary>
/// View model containing all computed data needed to render a profile
/// </summary>
public sealed record ProfileViewModel
{
    public required Domain.Entities.Profile Profile { get; init; }
    public required User User { get; init; }
    public required GuildMember GuildMember { get; init; }

    // Display data
    public required string AvatarUrl { get; init; }
    public required string Username { get; init; }
    public required string Discriminator { get; init; }
    public required string Description { get; init; }

    // Rank/Level data
    public required long ServerRank { get; init; }
    public required long GlobalRank { get; init; }
    public required int ServerLevel { get; init; }
    public required int GlobalLevel { get; init; }
    public required long ServerXp { get; init; }
    public required long GlobalXp { get; init; }

    // Bento game data
    public required bool HasBentoData { get; init; }
    public required int BentoCount { get; init; }
    public required long BentoRank { get; init; }
    public required long TotalBentoUsers { get; init; }

    // Statistics
    public required double TotalUserCount { get; init; }
    public required int GuildUserCount { get; init; }

    // Timezone/Birthday
    public required string TimezoneDisplay { get; init; }
    public required string BirthdayDisplay { get; init; }

    // Emotes
    public required string[] Emotes { get; init; }

    // Boards
    public required string? LastFmBoardHtml { get; init; }
    public required string? XpBoardHtml { get; init; }
    public required bool HasLastFmBoard { get; init; }
    public required bool HasXpBoard { get; init; }

    // Layout
    public required ProfileLayoutCalculator.LayoutResult Layout { get; init; }

    // Styling
    public required ProfileColors Colors { get; init; }
}

/// <summary>
/// Holds all color values with opacity applied
/// </summary>
public sealed record ProfileColors
{
    public required string Background { get; init; }
    public required string Description { get; init; }
    public required string Overlay { get; init; }
    public required string Sidebar { get; init; }
    public required string FmDivBackground { get; init; }
    public required string FmSongText { get; init; }
    public required string FmArtistText { get; init; }
    public required string XpDivBackground { get; init; }
    public required string XpText { get; init; }
    public required string XpText2 { get; init; }
    public required string XpBar { get; init; }
    public required string XpBar2 { get; init; }
    public required string XpDoneServerColor1 { get; init; }
    public required string XpDoneServerColor2 { get; init; }
    public required string XpDoneServerColor3 { get; init; }
    public required string XpDoneGlobalColor1 { get; init; }
    public required string XpDoneGlobalColor2 { get; init; }
    public required string XpDoneGlobalColor3 { get; init; }
}
