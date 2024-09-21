using Discord;
using Discord.Interactions;
using dotBento.Domain.Entities;
using dotBento.Domain.Extensions;
using dotBento.Infrastructure.Commands;

namespace dotBento.Bot.AutoCompleteHandlers;

public class SearchRemindersAutoComplete(ReminderCommands reminderCommands) : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var results = new List<string>();
        var reminders = await reminderCommands.GetRemindersAsync((long)context.User.Id);
        if (reminders.IsFailure)
        {
            return await Task.FromResult(AutocompletionResult.FromSuccess(results.Select(s => new AutocompleteResult(s, s))));
        }
        
        if (autocompleteInteraction.Data?.Current?.Value == null ||
            string.IsNullOrWhiteSpace(autocompleteInteraction.Data?.Current?.Value.ToString()))
        {
            results.ReplaceOrAddToList(reminders.Value.Select(CreateReminderString));
        }
        else
        {
            var searchValue = autocompleteInteraction.Data.Current.Value.ToString();
            results.ReplaceOrAddToList(reminders.Value.Where(x => x.Content.StartsWith(searchValue ?? "")).Select(CreateReminderString));
        }

        return await Task.FromResult(
            AutocompletionResult.FromSuccess(results.Take(25).Select(s => new AutocompleteResult(s, s))));
    }

    private static string CreateReminderString(Reminder reminder) =>
        $"{reminder.Id}, {reminder.Content.TruncateLongString(50)}";
}