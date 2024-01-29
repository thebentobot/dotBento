namespace dotBento.Domain.Entities.LastFm;

public sealed record BentoLastFmRecentTracksWithTotalTracks(
    IReadOnlyList<BentoLastFmRecentTrack> RecentTracks,
    int TotalTracks);