using dotBento.EntityFramework.Entities;
using dotBento.Domain.Entities.Tags;

namespace dotBento.Infrastructure.Extensions;

public static class TagExtensions
{
    public static BentoTags ToBentoTag(this Tag tag)
    {
        return new BentoTags(
            tag.TagId,
            tag.UserId,
            tag.GuildId,
            tag.Date,
            tag.Command,
            tag.Content,
            tag.Count);
    }
}