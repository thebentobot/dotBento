using System.Text.Json;
using System.Text.Json.Serialization;

namespace dotBento.CommandDocs;

public sealed record CommandManifest(int SchemaVersion, IReadOnlyList<CommandEntry> Commands);

public sealed record CommandEntry(
    string Id,
    IReadOnlyList<string> Path,
    string Invocation,
    string Description,
    IReadOnlyList<GroupEntry> GroupPath,
    bool GuildOnly,
    IReadOnlyList<string> RequiredUserPermissions,
    IReadOnlyList<OptionEntry> Options);

public sealed record GroupEntry(string Name, string Description);

public sealed record OptionEntry(
    string Name,
    string Description,
    string Type,
    bool Required,
    object? DefaultValue,
    IReadOnlyList<ChoiceEntry> Choices,
    bool Autocomplete,
    double? MinValue,
    double? MaxValue,
    int? MinLength,
    int? MaxLength,
    IReadOnlyList<string> ChannelTypes);

public sealed record ChoiceEntry(string Name, object Value);

public static class ManifestJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(CommandManifest manifest) =>
        JsonSerializer.Serialize(manifest, Options).ReplaceLineEndings("\n") + "\n";
}
