namespace dotBento.Infrastructure.Models.LastFm.TopArtists;

public sealed record TopArtistsResponse(TopArtistsWithUserAttributes? TopArtists, string? Message, int? Error);