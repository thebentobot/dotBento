using System.Text.Json.Serialization;

namespace dotBento.Infrastructure.Models.BentoMedia;

public sealed record MediaResolveResponse(
    [property: JsonPropertyName("platform")] string Platform,
    [property: JsonPropertyName("source_url")] string SourceUrl,
    [property: JsonPropertyName("posted_at")] DateTime? PostedAt,
    [property: JsonPropertyName("author")] MediaAuthor Author,
    [property: JsonPropertyName("content")] MediaContent Content
);

public sealed record MediaAuthor(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("display_name")] string? DisplayName
);

public sealed record MediaContent(
    [property: JsonPropertyName("caption")] string? Caption,
    [property: JsonPropertyName("attachments")] IReadOnlyList<MediaAttachment> Attachments
);

public sealed record MediaAttachment(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("content_type")] string ContentType,
    [property: JsonPropertyName("proxy")] bool Proxy
);
