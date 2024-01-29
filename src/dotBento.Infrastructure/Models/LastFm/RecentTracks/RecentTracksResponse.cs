namespace dotBento.Infrastructure.Models.LastFm.RecentTracks;

public sealed record RecentTracksResponse(
    RecentTracksWithUserAttributes? RecentTracks, 
    string? Message,
    int? Error);