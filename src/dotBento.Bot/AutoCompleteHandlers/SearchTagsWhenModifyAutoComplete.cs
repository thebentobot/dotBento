using CSharpFunctionalExtensions;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Services;
using dotBento.Infrastructure.Commands;

namespace dotBento.Bot.AutoCompleteHandlers;

public sealed class SearchTagsWhenModifyAutoComplete(TagCommands tagCommands, GuildMemberLookupService memberLookup) : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public async ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
    {
        var guildUser = context.Guild is not null
            ? await memberLookup.GetOrFetchAsync(context.Guild.Id, context.User.Id, context.Guild)
            : null;
        var hasManageMessages = guildUser is not null
            && context.Guild is not null
            && guildUser.HasGuildPermission(context.Guild, Permissions.ManageMessages);
        var authorId = hasManageMessages
            ? Maybe<long>.None
            : (long)context.User.Id;
        var results = await tagCommands.FindTagNamesForAutocompleteAsync(
            (long)context.Guild!.Id,
            option.Value?.ToString(),
            authorId);

        return results.Select(s => new ApplicationCommandOptionChoiceProperties(s, s));
    }
}
