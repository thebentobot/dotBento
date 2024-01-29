namespace dotBento.Domain.Entities.LastFm;

public sealed record BentoLastFmRecentTrack(
    bool NowPlaying,
    string Artist,
    string Track,
    string Album,
    string Image,
    string Url,
    DateTimeOffset? Date);