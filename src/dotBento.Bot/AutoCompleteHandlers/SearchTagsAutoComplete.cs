using CSharpFunctionalExtensions;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using dotBento.Infrastructure.Commands;

namespace dotBento.Bot.AutoCompleteHandlers;

public sealed class SearchTagsAutoComplete(TagCommands tagCommands) : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public async ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
    {
        var results = await tagCommands.FindTagNamesForAutocompleteAsync(
            (long)context.Guild!.Id,
            option.Value?.ToString(),
            Maybe<long>.None);

        return results.Select(s => new ApplicationCommandOptionChoiceProperties(s, s));
    }
}
