using dotBento.EntityFramework.Entities;
using dotBento.WebApi.Dtos;

namespace dotBento.WebApi.Extensions;

public static class InformationExtensions
{
    public static LeaderboardUserDto ToLeaderboardUserDto(this User user, int rank) =>
        new(
            rank + 1,
            user.UserId,
            user.Level,
            user.Xp,
            user.Username,
            user.Discriminator,
            user.AvatarUrl);
    
    public static LeaderboardUserDto ToLeaderboardUserDto(this GuildMember guildMember, int rank) =>
        new(
            rank + 1,
            guildMember.UserId,
            guildMember.Level,
            guildMember.Xp,
            guildMember.User.Username,
            guildMember.User.Discriminator,
            guildMember.AvatarUrl);
    
    public static PatreonUserDto ToPatreonUserDto(this Patreon patreon) =>
        new(
            patreon.UserId,
            patreon.Name,
            patreon.Avatar,
            patreon.Sponsor,
            patreon.Disciple,
            patreon.Enthusiast,
            patreon.Follower,
            patreon.Supporter
        );
}