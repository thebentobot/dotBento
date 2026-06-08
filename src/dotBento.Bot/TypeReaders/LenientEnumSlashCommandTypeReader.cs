using System.Reflection;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace dotBento.Bot.TypeReaders;

/// <summary>
/// A slash command type reader for enum parameters that accepts:
///   1. Integer values   ("0", "1") — NetCord's format
///   2. Enum member names ("Rock", "SevenDays") — case-insensitive
///   3. SlashCommandChoice display names ("7 Days", "Top Artists") — previous library's old string format
/// This handles the transition from the platform NET (string choices) to NetCord (integer choices)
/// and Discord client-side caching of old command definitions.
/// </summary>
public sealed class LenientEnumSlashCommandTypeReader<TContext> : SlashCommandTypeReader<TContext>
    where TContext : IApplicationCommandContext
{
    public override ApplicationCommandOptionType Type => ApplicationCommandOptionType.Integer;

    public override ValueTask<SlashCommandTypeReaderResult> ReadAsync(
        string value,
        TContext context,
        SlashCommandParameter<TContext> parameter,
        ApplicationCommandServiceConfiguration<TContext> configuration,
        IServiceProvider? serviceProvider)
    {
        var result = TryReadEnum(value, parameter.NonNullableType);
        return new(result is not null
            ? SlashCommandTypeReaderResult.Success(result)
            : SlashCommandTypeReaderResult.ParseFail(parameter.Name));
    }

    internal static object? TryReadEnum(string value, Type enumType)
    {
        // 1. Integer value (NetCord format: "0", "1", "2")
        if (long.TryParse(value, out var longVal))
        {
            var enumVal = Enum.ToObject(enumType, longVal);
            if (Enum.IsDefined(enumType, enumVal))
                return enumVal;
        }

        // 2. Enum member name, case-insensitive ("Rock", "SevenDays")
        if (Enum.TryParse(enumType, value, ignoreCase: true, out var parsedEnumVal)
            && parsedEnumVal is not null
            && Enum.IsDefined(enumType, parsedEnumVal))
        {
            return parsedEnumVal;
        }

        // 3. SlashCommandChoice display name, case-insensitive ("7 Days", "Top Artists")
        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var attr = field.GetCustomAttribute<SlashCommandChoiceAttribute>();
            if (attr?.Name != null && string.Equals(attr.Name, value, StringComparison.OrdinalIgnoreCase))
                return field.GetValue(null)!;
        }

        return null;
    }

    public override IChoicesProvider<TContext>? ChoicesProvider => EnumChoicesProvider<TContext>.Instance;
}

internal sealed class EnumChoicesProvider<TContext> : IChoicesProvider<TContext>
    where TContext : IApplicationCommandContext
{
    public static readonly EnumChoicesProvider<TContext> Instance = new();

    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        SlashCommandParameter<TContext> parameter)
    {
        var enumType = parameter.NonNullableType;
        var choices = enumType.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f =>
            {
                var attr = f.GetCustomAttribute<SlashCommandChoiceAttribute>();
                var name = attr?.Name ?? f.Name;
                var value = Convert.ToInt64(f.GetRawConstantValue());
                return new ApplicationCommandOptionChoiceProperties(name, value);
            });

        return new((IEnumerable<ApplicationCommandOptionChoiceProperties>?)choices);
    }
}
