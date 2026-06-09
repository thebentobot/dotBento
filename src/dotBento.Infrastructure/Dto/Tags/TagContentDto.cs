using Discord;

namespace dotBento.Infrastructure.Dto.Tags;

public sealed record TagContentDto(string? MessageContent, IAttachment?[] Attachments);