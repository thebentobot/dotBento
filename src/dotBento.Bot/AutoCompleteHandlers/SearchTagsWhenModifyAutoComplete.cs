using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using dotBento.Infrastructure.Commands;
using dotBento.Bot.Services;

namespace dotBento.Bot.AutoCompleteHandlers;

public sealed class SearchTagsWhenModifyAutoComplete(TagCommands tagCommands, IDiscordGuildMemberResolver guildMembers) : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        if (context.Guild is null)
            return AutocompletionResult.FromSuccess([]);
        var canManage = await GuildMemberPermissions.HasAsync(guildMembers, context.Guild, context.User, GuildPermission.ManageMessages);
        var userId = canManage ? Maybe<long>.None : (long)context.User.Id;
        var searchValue = autocompleteInteraction.Data?.Current?.Value?.ToString();
        var results = await tagCommands.FindTagNamesForAutocompleteAsync(
            (long)context.Guild.Id,
            userId,
            searchValue);

        return AutocompletionResult.FromSuccess(results.Select(s => new AutocompleteResult(s, s)));
    }
}
