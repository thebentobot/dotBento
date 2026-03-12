using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace dotBento.Bot.AutoCompleteHandlers;

public sealed class TimezoneAutoComplete : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
    {
        var zones = TimeZoneInfo.GetSystemTimeZones();

        var nowUtc = DateTimeOffset.UtcNow;
        var query = option.Value?.ToString();
        query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();

        IEnumerable<TimeZoneInfo> filtered = zones;
        if (!string.IsNullOrEmpty(query))
        {
            filtered = zones.Where(z => StartsWithQuery(z, query!))
                .Concat(zones.Where(z => MatchesQuery(z, query!) && !StartsWithQuery(z, query!)))
                .Distinct();
        }

        var results = (from z in filtered.Take(25)
            let offset = z.GetUtcOffset(nowUtc)
            let abbr = z.IsDaylightSavingTime(nowUtc) ? z.DaylightName : z.StandardName
            let name = $"{z.Id} ({abbr}, {FormatOffset(offset)})"
            select new ApplicationCommandOptionChoiceProperties(name, z.Id))
            .AsEnumerable<ApplicationCommandOptionChoiceProperties>();

        return ValueTask.FromResult(results);
    }

    private static string FormatOffset(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        offset = offset.Duration();
        return $"UTC{sign}{offset:hh\\:mm}";
    }

    private static bool StartsWithQuery(TimeZoneInfo z, string q) =>
        z.Id.StartsWith(q, StringComparison.OrdinalIgnoreCase)
        || z.DisplayName.StartsWith(q, StringComparison.OrdinalIgnoreCase)
        || z.StandardName.StartsWith(q, StringComparison.OrdinalIgnoreCase)
        || z.DaylightName.StartsWith(q, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesQuery(TimeZoneInfo z, string q) =>
        z.Id.Contains(q, StringComparison.OrdinalIgnoreCase)
        || z.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase)
        || z.StandardName.Contains(q, StringComparison.OrdinalIgnoreCase)
        || z.DaylightName.Contains(q, StringComparison.OrdinalIgnoreCase);
}
