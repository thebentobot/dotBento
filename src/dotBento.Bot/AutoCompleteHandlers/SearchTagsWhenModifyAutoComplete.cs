using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using dotBento.Domain.Extensions;
using dotBento.Infrastructure.Commands;

namespace dotBento.Bot.AutoCompleteHandlers;

public sealed class SearchTagsWhenModifyAutoComplete(TagCommands tagCommands) : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var guildUser = await context.Guild.GetUserAsync(context.User.Id);
        var userId = guildUser.GuildPermissions.ManageMessages ? (long)context.User.Id : Maybe<long>.None;
        var searchValue = autocompleteInteraction.Data?.Current?.Value?.ToString();
        var results = await tagCommands.FindTagNamesForAutocompleteAsync(
            (long)context.Guild.Id,
            userId,
            searchValue);

        return AutocompletionResult.FromSuccess(results.Select(s => new AutocompleteResult(s, s)));
    }
}
