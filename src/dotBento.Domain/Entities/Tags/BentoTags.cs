namespace dotBento.Domain.Entities.Tags;

public record BentoTags(long TagId, long UserId, long GuildId, DateTime? Date, string Command, string Content, int Count);