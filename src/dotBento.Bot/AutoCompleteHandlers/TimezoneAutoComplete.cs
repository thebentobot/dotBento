using Discord;
using Discord.Interactions;

namespace dotBento.Bot.AutoCompleteHandlers;

public sealed class TimezoneAutoComplete : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        var zones = TimeZoneInfo.GetSystemTimeZones();

        var nowUtc = DateTimeOffset.UtcNow;
        var query = autocompleteInteraction.Data?.Current?.Value?.ToString();
        query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();

        IEnumerable<TimeZoneInfo> filtered = zones;
        if (!string.IsNullOrEmpty(query))
        {
            filtered = zones.Where(z => z.Id.StartsWith(query!, StringComparison.OrdinalIgnoreCase)
                                        || z.DisplayName.StartsWith(query!, StringComparison.OrdinalIgnoreCase))
                .Concat(
                    zones.Where(z => (z.Id.Contains(query!, StringComparison.OrdinalIgnoreCase)
                                      || z.DisplayName.Contains(query!, StringComparison.OrdinalIgnoreCase))
                                     && !z.Id.StartsWith(query!, StringComparison.OrdinalIgnoreCase)
                                     && !z.DisplayName.StartsWith(query!, StringComparison.OrdinalIgnoreCase)))
                .Distinct();
        }

        static string FormatOffset(TimeSpan offset)
        {
            var sign = offset < TimeSpan.Zero ? "-" : "+";
            offset = offset.Duration();
            return $"UTC{sign}{offset:hh\\:mm}";
        }

        var results = (from z in filtered.Take(25)
            let offset = z.GetUtcOffset(nowUtc)
            let name = $"{z.Id} ({FormatOffset(offset)})"
            select new AutocompleteResult(name, z.Id)).ToList();

        return await Task.FromResult(AutocompletionResult.FromSuccess(results));
    }
}