using NetCord;

namespace dotBento.Infrastructure.Dto.Tags;

public sealed record TagContentDto(string? MessageContent, Attachment?[] Attachments);
