namespace dotBento.Infrastructure.Models.LastFm.TopAlbums;

public sealed record TopAlbumsResponse(TopAlbumsList? TopAlbums, string? Message, int? Error);