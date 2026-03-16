using CSharpFunctionalExtensions;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using dotBento.Domain.Extensions;
using dotBento.Infrastructure.Commands;

namespace dotBento.Bot.AutoCompleteHandlers;

public sealed class SearchTagsAutoComplete(TagCommands tagCommands) : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public async ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
    {
        var results = new List<string>();
        var tags = await tagCommands.FindTagsAsync((long)context.Guild!.Id, true, Maybe<long>.None);
        if (tags.IsFailure)
        {
            return results.Select(s => new ApplicationCommandOptionChoiceProperties(s, s));
        }

        if (option.Value == null || string.IsNullOrWhiteSpace(option.Value.ToString()))
        {
            results.ReplaceOrAddToList(tags.Value.Select(s => s.Command));
        }
        else
        {
            var searchValue = option.Value.ToString();
            results.ReplaceOrAddToList(tags.Value.Where(x => x.Command.StartsWith(searchValue ?? "")).Select(s => s.Command));
        }

        return results.Take(25).Select(s => new ApplicationCommandOptionChoiceProperties(s, s));
    }
}
