using dotBento.EntityFramework.Entities;
using dotBento.WebApi.Dtos;

namespace dotBento.WebApi.Extensions;

public static class InformationExtensions
{
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
