using System.Diagnostics;
using Discord;
using Discord.WebSocket;
using dotBento.Domain;
using dotBento.EntityFramework.Context;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace dotBento.Bot.Services;

public sealed class BackgroundService(UserService userService,
    GuildService guildService,
    DiscordSocketClient client,
    SupporterService supporterService,
    BotListService botListService,
    ReminderCommands reminderCommands,
    IDbContextFactory<BotDbContext> contextFactory,
    IDiscordUserResolver userResolver,
    IDmSender dmSender)
{
    public void QueueJobs()
    {
        Log.Information($"RecurringJob: Adding {nameof(UpdateMetrics)}");
        RecurringJob.AddOrUpdate(nameof(UpdateMetrics), () => UpdateMetrics(), "* * * * *");

        Log.Information($"RecurringJob: Adding {nameof(UpdateStatus)}");
        RecurringJob.AddOrUpdate(nameof(UpdateStatus), () => UpdateStatus(), "*/5 * * * *");

        Log.Information($"RecurringJob: Adding {nameof(ClearUserCache)}");
        RecurringJob.AddOrUpdate(nameof(ClearUserCache), () => ClearUserCache(), "30 */2 * * *");

        Log.Information($"RecurringJob: Adding {nameof(SendRemindersToUsers)}");
        RecurringJob.AddOrUpdate(nameof(SendRemindersToUsers), () => SendRemindersToUsers(), "* * * * *");

        Log.Information($"RecurringJob: Adding {nameof(UpdateGuildMemberCounts)}");
        RecurringJob.AddOrUpdate(nameof(UpdateGuildMemberCounts), () => UpdateGuildMemberCounts(), "0 * * * *");

        Log.Information($"RecurringJob: Adding {nameof(UpdateLeaderboardUserAvatars)}");
        RecurringJob.AddOrUpdate(nameof(UpdateLeaderboardUserAvatars), () => UpdateLeaderboardUserAvatars(), "0 */6 * * *");
        
        Log.Information($"RecurringJob: Adding {nameof(UpdateBotLists)}");
        RecurringJob.AddOrUpdate(nameof(UpdateBotLists), () => UpdateBotLists(), "*/10 * * * *");
    }

    public async Task UpdateStatus()
    {
        Log.Information($"Running {nameof(UpdateStatus)}");
        var activity = GetRandomActivityStatus(client);
        await client.SetActivityAsync(activity);
    }

    private static Game GetRandomActivityStatus(DiscordSocketClient client)
    {
        var guildCount = client.Guilds.Count;
        var userCount = client.Guilds.Sum(x => x.MemberCount);

        var formattedUserCount = FormatThousandsCount(userCount);
        var formattedGuildCount = FormatThousandsCount(guildCount);

        var statusText = $"{userCount} {(userCount == 1 ? "user" : "users")} on {formattedGuildCount} {(guildCount == 1 ? "server" : "servers")}";
        return new Game(statusText, ActivityType.Watching);
    }

    private static string FormatThousandsCount(int count)
    {
        if (count >= 1_000_000)
            return $"{count / 1_000_000.0:0.0}M";
        if (count >= 1_000)
            return $"{count / 1_000.0:0.0}k";
        return count.ToString();
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

            var user = await userResolver.GetUserAsync((ulong)reminder.UserId);
            if (user is null)
            {
                Log.Warning(
                    "User {UserId} could not be resolved when attempting to send reminder {ReminderId}. Deleting reminder.",
                    reminder.UserId, reminder.Id);
                await reminderCommands.DeleteReminderAsync(reminder.UserId, reminder.Id);
                continue;
            }

            try
            {
                var result = await dmSender.SendReminderAsync(user, reminder.Content);
                switch (result)
                {
                    case DmSendResult.Success:
                        await reminderCommands.DeleteReminderAsync(reminder.UserId, reminder.Id);
                        break;
                    case DmSendResult.Forbidden:
                        Log.Warning(
                            "Cannot send reminder {ReminderId} to user {UserId} due to permission/DM restrictions. Deleting reminder.",
                            reminder.Id, reminder.UserId);
                        await reminderCommands.DeleteReminderAsync(reminder.UserId, reminder.Id);
                        break;
                }
            }
            catch (Exception ex)
            {
                // Unexpected error: log and keep the reminder so it can be retried later by the next run
                Log.Error(ex,
                    "Unexpected error sending reminder {ReminderId} to user {UserId}. Reminder will not be deleted to allow retry.",
                    reminder.Id, reminder.UserId);
            }
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

    public async Task UpdateGuildMemberCounts()
    {
        Log.Information($"Running {nameof(UpdateGuildMemberCounts)}");

        try
        {
            if (client.Guilds?.Count == null)
            {
                Log.Information($"Client guild count is null, cancelling {nameof(UpdateGuildMemberCounts)}");
                return;
            }

            foreach (var guild in client.Guilds)
            {
                await guildService.UpdateGuildMemberCountAsync(guild.Id, guild.MemberCount);
            }

            Log.Information($"Updated member counts for {client.Guilds.Count} guilds");
        }
        catch (Exception e)
        {
            Log.Error(e, nameof(UpdateGuildMemberCounts));
            throw;
        }
    }

    public async Task UpdateLeaderboardUserAvatars()
    {
        Log.Information($"Running {nameof(UpdateLeaderboardUserAvatars)}");

        try
        {
            await using var db = await contextFactory.CreateDbContextAsync();
            var topUsers = await db.Users
                .OrderByDescending(u => u.Level)
                .ThenByDescending(u => u.Xp)
                .Take(50)
                .ToListAsync();

            Log.Information($"Found {topUsers.Count} users in the global leaderboard");

            var updatedCount = 0;
            foreach (var user in topUsers)
            {
                try
                {
                    var discordUser = client.GetUser((ulong)user.UserId);
                    if (discordUser == null) continue;
                    await userService.UpdateUserAvatarAsync(discordUser);
                    updatedCount++;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, $"Failed to update avatar for user {user.UserId}");
                }
            }

            Log.Information($"Updated avatars for {updatedCount} users in the global leaderboard");
        }
        catch (Exception e)
        {
            Log.Error(e, nameof(UpdateLeaderboardUserAvatars));
            throw;
        }
    }
    
    public async Task UpdateBotLists()
    {
        await botListService.UpdateBotLists(client.Guilds.Count);
    }
}
