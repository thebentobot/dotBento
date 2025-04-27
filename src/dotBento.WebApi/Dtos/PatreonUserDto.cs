namespace dotBento.WebApi.Dtos;

public record PatreonUserDto(
    long UserId,
    string Name,
    string AvatarUrl,
    bool Sponsor,
    bool Disciple,
    bool Enthusiast,
    bool Follower,
    bool Supporter
);