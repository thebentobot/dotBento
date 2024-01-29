namespace dotBento.Domain.Entities.LastFm;

public sealed record BentoLastFmTopArtist(
    string Name,
    string? ImageUrl,
    string Url,
    int PlayCount,
    int Rank);