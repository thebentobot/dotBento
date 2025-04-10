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
        var results = new List<string>();
        var tags = await tagCommands.FindTagsAsync((long)context.Guild.Id, true, Maybe<long>.None);
        if (tags.IsFailure)
        {
            return await Task.FromResult(AutocompletionResult.FromSuccess(results.Select(s => new AutocompleteResult(s, s))));
        }
        
        if (autocompleteInteraction.Data?.Current?.Value == null ||
            string.IsNullOrWhiteSpace(autocompleteInteraction.Data?.Current?.Value.ToString()))
        {
            results.ReplaceOrAddToList(tags.Value.Select(s => s.Command));
        }
        else
        {
            var searchValue = autocompleteInteraction.Data.Current.Value.ToString();
            results.ReplaceOrAddToList(tags.Value.Where(x => x.Command.StartsWith(searchValue ?? "")).Select(s => s.Command));
        }

        return await Task.FromResult(
            AutocompletionResult.FromSuccess(results.Take(25).Select(s => new AutocompleteResult(s, s))));
    }
}