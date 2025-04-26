namespace dotBento.WebApi.Dtos;

public record LeaderboardUserDto(
    int Rank,
    long UserId,
    int Level,
    int Xp,
    string Username,
    string Discriminator,
    string AvatarUrl
);