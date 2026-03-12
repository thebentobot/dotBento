using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using dotBento.Domain.Entities;
using dotBento.Domain.Extensions;
using dotBento.Infrastructure.Commands;

namespace dotBento.Bot.AutoCompleteHandlers;

public sealed class SearchRemindersAutoComplete(ReminderCommands reminderCommands) : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public async ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
    {
        var results = new List<string>();
        var reminders = await reminderCommands.GetRemindersAsync((long)context.User.Id);
        if (reminders.IsFailure)
        {
            return results.Select(s => new ApplicationCommandOptionChoiceProperties(s, s));
        }

        if (option.Value == null || string.IsNullOrWhiteSpace(option.Value.ToString()))
        {
            results.ReplaceOrAddToList(reminders.Value.Select(CreateReminderString));
        }
        else
        {
            var searchValue = option.Value.ToString();
            results.ReplaceOrAddToList(reminders.Value.Where(x => x.Content.StartsWith(searchValue ?? "")).Select(CreateReminderString));
        }

        return results.Take(25).Select(s => new ApplicationCommandOptionChoiceProperties(s, s));
    }

    private static string CreateReminderString(Reminder reminder) =>
        $"{reminder.Id}, {reminder.Content.TruncateLongString(50)}";
}
