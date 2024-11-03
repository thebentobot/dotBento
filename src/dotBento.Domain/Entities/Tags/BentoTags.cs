namespace dotBento.Domain.Entities.Tags;

public sealed record BentoTags(long TagId, long UserId, long GuildId, DateTime? Date, string Command, string Content, int Count);