namespace dotBento.Domain.Entities.LastFm;

public sealed record BentoLastFmUserInfo(
    string Name,
    string? ImageUrl,
    string Url,
    string? Country,
    int PlayCount,
    int ArtistCount,
    int AlbumCount,
    int TrackCount,
    DateTimeOffset RegisteredAt);