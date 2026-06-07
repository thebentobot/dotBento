using CSharpFunctionalExtensions;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Services;
using dotBento.Domain.Extensions;
using dotBento.Infrastructure.Commands;

namespace dotBento.Bot.AutoCompleteHandlers;

public sealed class SearchTagsWhenModifyAutoComplete(TagCommands tagCommands, GuildMemberLookupService memberLookup) : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public async ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
    {
        var results = new List<string>();
        var guildUser = context.Guild is not null
            ? await memberLookup.GetOrFetchAsync(context.Guild.Id, context.User.Id, context.Guild)
            : null;
        var hasManageMessages = guildUser is not null
            && context.Guild is not null
            && guildUser.HasGuildPermission(context.Guild, Permissions.ManageMessages);
        var authorId = hasManageMessages
            ? Maybe<long>.None
            : (long)context.User.Id;
        var tags = await tagCommands.FindTagsAsync((long)context.Guild!.Id, true, authorId);
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
