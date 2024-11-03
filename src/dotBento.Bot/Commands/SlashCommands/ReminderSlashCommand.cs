using System.Globalization;
using Discord;
using Discord.Interactions;
using dotBento.Bot.AutoCompleteHandlers;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("remind", "Manage reminders for yourself")]
public sealed class ReminderSlashCommand(InteractiveService interactiveService, ReminderCommand reminderCommand)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("create", "Create a reminder")]
    public async Task CreateCommand(
        [Summary("content", "What do you need to be reminded of")] string content,
        [Summary("date", "Write a date for the reminder")] string date
    )
    {
        var parseDate = date.ToString(CultureInfo.InvariantCulture);
        var convertDate = parseDate.ParseDateTimeOffset();
        if (convertDate.HasNoValue)
        {
            await Context.SendResponse(interactiveService,
                ErrorEmbed("Invalid date. Please provide a valid date in the format `YYYY-MM-DDThh:mm[{+|-}hh:mm]` (e.g. 2022-12-31T23:59+00:00)."));
            return;
        }
        await Context.SendResponse(
            interactiveService,
            await reminderCommand.CreateReminderAsync((long)Context.User.Id, content, convertDate.Value),
            true
        );
    }

    [SlashCommand("delete", "Delete a reminder")]
    public async Task DeleteCommand(
        [Summary("reminderId", "Select a reminder")] [Autocomplete(typeof(SearchRemindersAutoComplete))] string reminder
    )
    {
        var reminderId = int.Parse(reminder.Split(",")[0]);
        await Context.SendResponse(
            interactiveService,
            await reminderCommand.DeleteReminderAsync((long)Context.User.Id, reminderId),
            true
        );
    }

    [SlashCommand("update", "Update a reminder")]
    public async Task UpdateCommand(
        [Summary("reminderId", "Select a reminder")] [Autocomplete(typeof(SearchRemindersAutoComplete))] string reminder,
        [Summary("newContent", "Write a new content for the reminder")] string? newContent = null,
        [Summary("newDate", "Write a new date for the reminder")] string? newDate = null
    )
    {
        if (newDate != null)
        {
            var parseDate = newDate.ParseDateTimeOffset();
            if (parseDate.HasNoValue)
            {
                await Context.SendResponse(
                    interactiveService,
                    ErrorEmbed("Invalid date. Please provide a valid date in the format `YYYY-MM-DDThh:mm[{+|-}hh:mm]` (e.g. 2022-12-31T23:59+00:00)."),
                    true);
                return;
            }
            var reminderId = int.Parse(reminder.Split(",")[0]);
            await Context.SendResponse(
                interactiveService,
                await reminderCommand.UpdateReminderAsync((long)Context.User.Id, reminderId, newContent, parseDate.Value),
                true
            );
        } else
        {
            var reminderId = int.Parse(reminder.Split(",")[0]);
            await Context.SendResponse(
                interactiveService,
                await reminderCommand.UpdateReminderAsync((long)Context.User.Id, reminderId, newContent, null),
                true
            );
        }
    }

    [SlashCommand("list", "List all your reminders")]
    public async Task ListCommand() =>
        await Context.SendResponse(
            interactiveService,
            await reminderCommand.GetRemindersAsync((long)Context.User.Id),
            true
        );
    
    [SlashCommand("info", "Get information about a reminder")]
    public async Task InfoCommand(
        [Summary("reminderId", "Select a reminder")] [Autocomplete(typeof(SearchRemindersAutoComplete))] string reminder
    )
    {
        var reminderId = int.Parse(reminder.Split(",")[0]);
        await Context.SendResponse(
            interactiveService,
            await reminderCommand.GetReminderAsync((long)Context.User.Id, reminderId),
            true
        );
    }
    
    private static ResponseModel ErrorEmbed(string error)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle(error)
            .WithColor(Color.Red);
        return embed;
    }
}