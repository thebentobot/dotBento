using Discord;
using Discord.Interactions;
using dotBento.Bot.Extensions;
using dotBento.Domain.Extensions;
using dotBento.Infrastructure.Utilities;

namespace dotBento.Bot.AutoCompleteHandlers;

public class DateTimeAutoComplete : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var results = new List<string>();

        if (autocompleteInteraction.Data?.Current?.Value == null ||
            string.IsNullOrWhiteSpace(autocompleteInteraction.Data?.Current?.Value.ToString()))
        {
            results
                .ReplaceOrAddToList(LastFmTimePeriodUtilities.UserOptionsLastFmTimeSpanSlashCommand.Keys.ToList());
        }
        else
        {
            var searchValue = autocompleteInteraction.Data.Current.Value.ToString();

            results.ReplaceOrAddToList(LastFmTimePeriodUtilities.UserOptionsLastFmTimeSpanSlashCommand.Keys
                .Where(w => w.StartsWith(searchValue ?? throw new InvalidOperationException(), StringComparison.OrdinalIgnoreCase))
                .Take(6));

            results.ReplaceOrAddToList(LastFmTimePeriodUtilities.UserOptionsLastFmTimeSpanSlashCommand.Keys
                .Where(w => w.Contains(searchValue ?? throw new InvalidOperationException(), StringComparison.OrdinalIgnoreCase))
                .Take(4));
        }

        return await Task.FromResult(
            AutocompletionResult.FromSuccess(results.Select(s => new AutocompleteResult(s, s))));
    }
}