namespace dotBento.Domain.Entities.LastFm;

public sealed record BentoLastFmTopTrack(
    string Name,
    string Artist,
    string? ImageUrl,
    string Url,
    int PlayCount,
    int Rank);