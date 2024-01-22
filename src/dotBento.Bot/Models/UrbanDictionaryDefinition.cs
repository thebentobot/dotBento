using System.Text.Json.Serialization;

namespace dotBento.Bot.Models;

public sealed class UrbanDictionaryDefinition
{
    public string Definition { get; set; }
    public string Permalink { get; set; }
    [JsonPropertyName("thumbs_up")] public int ThumbsUp { get; set; }
    [JsonPropertyName("thumbs_down")] public int ThumbsDown { get; set; }
    public string Author { get; set; }
    public string Word { get; set; }
    [JsonPropertyName("written_on")] public DateTimeOffset WrittenOn { get; set; }
    public int Defid { get; set; }
    public string Example { get; set; }
}