using System.Text.Json.Serialization;

namespace dotBento.Infrastructure.Models.UrbanDictionary;

public sealed record UrbanDictionaryDefinition(
    string Definition,
    string Permalink,
    [property: JsonPropertyName("thumbs_up")] int ThumbsUp,
    [property: JsonPropertyName("thumbs_down")] int ThumbsDown,
    string Author,
    string Word,
    [property: JsonPropertyName("written_on")] DateTimeOffset WrittenOn,
    int Defid,
    string Example
);