namespace dotBento.Infrastructure.Models.LastFm.UserInfo;

public sealed record UserInfoResponse(UserInfo User, string? Message, int? Error);