using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using dotBento.Domain.Extensions;
using dotBento.Infrastructure.Commands;

namespace dotBento.Bot.AutoCompleteHandlers;

public class SearchTagsWhenModifyAutoComplete(TagCommands tagCommands) : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var results = new List<string>();
        var userId = context.Guild.GetUserAsync(context.User.Id).Result.GuildPermissions.ManageMessages ? (long) context.User.Id : Maybe<long>.None;
        var tags = await tagCommands.FindTagsAsync((long)context.Guild.Id, true, userId);
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
            AutocompletionResult.FromSuccess(results.Select(s => new AutocompleteResult(s, s))));
    }
}