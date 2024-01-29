namespace dotBento.Infrastructure.Models.LastFm.TopTracks;

public sealed record TopTracksResponse(TopTracksWithUserAttributes? TopTracks, string? Message, int? Error);