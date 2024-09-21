using System.Diagnostics;
using Discord;
using Discord.WebSocket;
using dotBento.Bot.Resources;
using dotBento.Domain;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services;
using Hangfire;
using Serilog;

namespace dotBento.Bot.Services;

public class BackgroundService(UserService userService,
    GuildService guildService,
    DiscordSocketClient client,
    SupporterService supporterService,
    ReminderCommands reminderCommands)
{
    public void QueueJobs()
    {
        Log.Information($"RecurringJob: Adding {nameof(UpdateMetrics)}");
        RecurringJob.AddOrUpdate(nameof(UpdateMetrics), () => UpdateMetrics(), "* * * * *");
        
        Log.Information($"RecurringJob: Adding {nameof(ClearUserCache)}");
        RecurringJob.AddOrUpdate(nameof(ClearUserCache), () => ClearUserCache(), "30 */2 * * *");

        Log.Information($"RecurringJob: Adding {nameof(SendRemindersToUsers)}");
        RecurringJob.AddOrUpdate(nameof(SendRemindersToUsers), () => SendRemindersToUsers(), "* * * * *");
    }

    public async Task SendRemindersToUsers()
    {
        Log.Information($"Running {nameof(SendRemindersToUsers)}");
        var reminders = await reminderCommands.GetAllRecentRemindersAsync();
        if (reminders.IsFailure)
        {
            return;
        }
        
        foreach (var reminder in reminders.Value)
        {
            var checkIfBentoUser = await userService.GetUserAsync((ulong)reminder.UserId);
            if (checkIfBentoUser.HasNoValue)
            {
                await reminderCommands.DeleteReminderAsync(reminder.UserId, reminder.Id);
                continue;
            }
            
            var user = await client.GetUserAsync((ulong)reminder.UserId);

            var dmChannel = await user.CreateDMChannelAsync();
            await dmChannel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(DiscordConstants.BentoYellow)
                .WithTitle("Reminder")
                .WithDescription(reminder.Content)
                .Build());
            await reminderCommands.DeleteReminderAsync(reminder.UserId, reminder.Id);
        }
    }
    
    public async Task UpdateMetrics()
    {
        Log.Information($"Running {nameof(UpdateMetrics)}");

        Statistics.RegisteredUserCount.Set(await userService.GetTotalDatabaseUserCountAsync());
        var discordUserCount = await userService.GetTotalDiscordUserCountAsync();
        Statistics.RegisteredDiscordUserCount.Set(discordUserCount.HasValue ? discordUserCount.Value : 0);
        Statistics.RegisteredGuildCount.Set(await guildService.GetTotalGuildCountAsync());
        Statistics.ActiveSupporterCount.Set(await supporterService.GetActiveSupporterCountAsync());

        try
        {
            if (client.Guilds?.Count == null)
            {
                Log.Information($"Client guild count is null, cancelling {nameof(UpdateMetrics)}");
                return;
            }

            var currentProcess = Process.GetCurrentProcess();
            var startTime = DateTime.Now - currentProcess.StartTime;

            if (startTime.Minutes > 8)
            {
                Statistics.DiscordServerCount.Set(client.Guilds.Count);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, nameof(UpdateMetrics));
            throw;
        }
    }
    
    public void ClearUserCache()
    {
        client.PurgeUserCache();
        Log.Information("Purged discord user cache");
    }
}