using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using dotBento.Domain.Extensions;
using dotBento.Infrastructure.Commands;

namespace dotBento.Bot.AutoCompleteHandlers;

public sealed class SearchTagsAutoComplete(TagCommands tagCommands) : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var searchValue = autocompleteInteraction.Data?.Current?.Value?.ToString();
        var results = await tagCommands.FindTagNamesForAutocompleteAsync(
            (long)context.Guild.Id,
            Maybe<long>.None,
            searchValue);

        return AutocompletionResult.FromSuccess(results.Select(s => new AutocompleteResult(s, s)));
    }
}
