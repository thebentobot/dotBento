using CSharpFunctionalExtensions;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Extensions;
using dotBento.Domain.Extensions;
using dotBento.Infrastructure.Commands;

namespace dotBento.Bot.AutoCompleteHandlers;

public sealed class SearchTagsWhenModifyAutoComplete(TagCommands tagCommands) : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public async ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
    {
        var results = new List<string>();
        var guildUser = context.Guild?.Users.GetValueOrDefault(context.User.Id);
        var userId = guildUser is not null && context.Guild is not null && guildUser.HasGuildPermission(context.Guild, Permissions.ManageMessages)
            ? (long)context.User.Id
            : Maybe<long>.None;
        var tags = await tagCommands.FindTagsAsync((long)context.Guild!.Id, true, userId);
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
