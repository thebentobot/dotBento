using Discord;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Infrastructure.Commands;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SharedCommands;

public sealed class ReminderCommand(ReminderCommands reminderCommands)
{
    public async Task<ResponseModel> CreateReminderAsync(long userId, string content, DateTimeOffset date)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Embed };
        var result = await reminderCommands.CreateReminderAsync(userId, content, date);
        if (result.IsFailure)
        {
            embed.Embed
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription(result.Error);
            return embed;
        }
        embed.Embed
            .WithColor(Color.Green)
            .WithTitle("Reminder created successfully.")
            .WithDescription($"A reminder `{content}` for <t:{date.ToUnixTimeSeconds()}:R> has been created.\nRemember to have DMs enabled to receive reminders.");
        return embed;
    }
    
    public async Task<ResponseModel> DeleteReminderAsync(long userId, int reminderId)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Embed };
        var result = await reminderCommands.DeleteReminderAsync(userId, reminderId);
        if (result.IsFailure)
        {
            embed.Embed
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription(result.Error);
            return embed;
        }
        embed.Embed
            .WithColor(Color.Green)
            .WithTitle("Reminder deleted successfully.")
            .WithDescription($"Reminder with ID `{reminderId}` has been deleted.");
        return embed;
    }
    
    public async Task<ResponseModel> UpdateReminderAsync(long userId, int reminderId, string? newContent, DateTimeOffset? newDate)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Embed };
        var result = await reminderCommands.UpdateReminderAsync(userId, reminderId, newContent, newDate);
        if (result.IsFailure)
        {
            embed.Embed
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription(result.Error);
            return embed;
        }
        var description = $"Reminder with ID `{reminderId}` has been updated.";
        if (newContent != null)
        {
            description += $"\nNew content: `{newContent}`";
        }
        if (newDate != null)
        {
            description += $"\nNew date: <t:{newDate.Value.ToUnixTimeSeconds()}:R>";
        }
        embed.Embed
            .WithColor(Color.Green)
            .WithTitle("Reminder updated successfully.\nRemember to have DMs enabled to receive reminders.")
            .WithDescription(description);
        return embed;
    }
    
    public async Task<ResponseModel> GetReminderAsync(long userId, int reminderId)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Embed };
        var result = await reminderCommands.GetReminderAsync(userId, reminderId);
        if (result.IsFailure)
        {
            embed.Embed
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription(result.Error);
            return embed;
        }
        var reminder = result.Value;
        embed.Embed
            .WithColor(DiscordConstants.BentoYellow)
            .WithTitle("Reminder")
            .WithDescription($"ID: `{reminder.Id}`\nContent: `{reminder.Content}`\nDate: <t:{reminder.Date.ToUnixTimeSeconds()}:R>");
        return embed;
    }
    
    public async Task<ResponseModel> GetRemindersAsync(long userId)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Embed };
        var result = await reminderCommands.GetRemindersAsync(userId);
        if (result.IsFailure)
        {
            embed.Embed
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription(result.Error);
            return embed;
        }
        var reminders = result.Value;

        var remindersPageChunks = reminders.ChunkBy(10);
        
        var pages = remindersPageChunks
            .Select(remindersPageChunk => new PageBuilder()
                .WithColor(DiscordConstants.BentoYellow)
                .WithFooter(new EmbedFooterBuilder()
                    { Text = $"{reminders.Count} {(reminders.Count != 1 ? "reminders" : "reminder")} found" })
                .WithDescription(string.Join("\n", remindersPageChunk.Select(x => $"ID: `{x.Id}`\nContent: `{x.Content}`\nDate: <t:{x.Date.ToUnixTimeSeconds()}:R>")))
            ).ToList();
        
        embed.StaticPaginator = pages.BuildSimpleStaticPaginator();
        embed.ResponseType = ResponseType.Paginator;
        
        return embed;
    }
}