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

        // Cleanup and sync jobs - run daily at different times to spread the load
        Log.Information($"RecurringJob: Adding {nameof(CleanupStaleGuilds)}");
        RecurringJob.AddOrUpdate(nameof(CleanupStaleGuilds), () => CleanupStaleGuilds(), "0 2 * * *"); // 2 AM daily

        Log.Information($"RecurringJob: Adding {nameof(CleanupStaleGuildMembers)}");
        RecurringJob.AddOrUpdate(nameof(CleanupStaleGuildMembers), () => CleanupStaleGuildMembers(), "0 3 * * *"); // 3 AM daily

        Log.Information($"RecurringJob: Adding {nameof(CleanupStaleUsers)}");
        RecurringJob.AddOrUpdate(nameof(CleanupStaleUsers), () => CleanupStaleUsers(), "0 4 * * *"); // 4 AM daily

        Log.Information($"RecurringJob: Adding {nameof(SyncUserData)}");
        RecurringJob.AddOrUpdate(nameof(SyncUserData), () => SyncUserData(), "0 5 * * *"); // 5 AM daily

        Log.Information($"RecurringJob: Adding {nameof(SyncGuildData)}");
        RecurringJob.AddOrUpdate(nameof(SyncGuildData), () => SyncGuildData(), "0 6 * * *"); // 6 AM daily

        Log.Information($"RecurringJob: Adding {nameof(SyncGuildMemberData)}");
        RecurringJob.AddOrUpdate(nameof(SyncGuildMemberData), () => SyncGuildMemberData(), "0 7 * * *"); // 7 AM daily
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

        var statusText = $"{formattedUserCount} {(userCount == 1 ? "user" : "users")} on {formattedGuildCount} {(guildCount == 1 ? "server" : "servers")}";
        return new Game(statusText, ActivityType.Watching);
    }

    private static string FormatThousandsCount(int count) =>
        count switch
        {
            >= 1_000_000 => TrimTrailingZero($"{count / 1_000_000.0:0.0}") + "M",
            >= 1_000 => TrimTrailingZero($"{count / 1_000.0:0.0}") + "k",
            _ => count.ToString()
        };

    private static string TrimTrailingZero(string value) =>
        value.EndsWith(".0")
            ? value.Substring(0, value.Length - 2)
            : value;

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

    /// <summary>
    /// Removes guilds from database that the bot is no longer a member of.
    /// Processes in batches to avoid memory issues.
    /// </summary>
    public async Task CleanupStaleGuilds()
    {
        Log.Information($"Running {nameof(CleanupStaleGuilds)}");

        try
        {
            var clientGuilds = client.Guilds.AsMaybe();
            if (clientGuilds.HasNoValue)
            {
                Log.Information($"Client guilds not available, cancelling {nameof(CleanupStaleGuilds)}");
                return;
            }

            const int batchSize = 50;
            var skip = 0;
            var totalProcessed = 0;
            var totalDeleted = 0;

            while (true)
            {
                var guilds = await guildService.GetGuildBatchAsync(batchSize, skip);
                if (guilds.Count == 0) break;

                foreach (var dbGuild in guilds)
                {
                    totalProcessed++;

                    // Check if bot is still in this guild
                    var maybeGuild = client.GetGuild((ulong)dbGuild.GuildId).AsMaybe();
                    if (maybeGuild.HasNoValue)
                    {
                        Log.Information($"Removing stale guild: {dbGuild.GuildName} ({dbGuild.GuildId})");
                        await guildService.RemoveGuildAsync((ulong)dbGuild.GuildId);
                        totalDeleted++;
                    }

                    // Rate limiting: small delay between checks
                    await Task.Delay(100);
                }

                skip += batchSize;

                // Longer delay between batches
                await Task.Delay(2000);
            }

            Log.Information($"Completed {nameof(CleanupStaleGuilds)}: Processed {totalProcessed} guilds, deleted {totalDeleted}");
        }
        catch (Exception e)
        {
            Log.Error(e, nameof(CleanupStaleGuilds));
            throw;
        }
    }

    /// <summary>
    /// Removes guild members from database who are no longer in their respective guilds.
    /// Processes in batches to avoid memory issues and rate limits.
    /// </summary>
    public async Task CleanupStaleGuildMembers()
    {
        Log.Information($"Running {nameof(CleanupStaleGuildMembers)}");

        try
        {
            var clientGuilds = client.Guilds.AsMaybe();
            if (clientGuilds.HasNoValue)
            {
                Log.Information($"Client guilds not available, cancelling {nameof(CleanupStaleGuildMembers)}");
                return;
            }

            const int batchSize = 100;
            var skip = 0;
            var totalProcessed = 0;
            var totalDeleted = 0;

            while (true)
            {
                var guildMembers = await guildService.GetGuildMemberBatchAsync(batchSize, skip);
                if (guildMembers.Count == 0) break;

                var guildMembersToDelete = new List<long>();

                foreach (var dbGuildMember in guildMembers)
                {
                    totalProcessed++;

                    // Check if guild still exists
                    var maybeGuild = client.GetGuild((ulong)dbGuildMember.GuildId).AsMaybe();
                    if (maybeGuild.HasNoValue)
                    {
                        // Guild doesn't exist, mark for deletion
                        guildMembersToDelete.Add(dbGuildMember.GuildMemberId);
                        continue;
                    }

                    // Check if user is still in the guild
                    var maybeGuildUser = maybeGuild.Value.GetUser((ulong)dbGuildMember.UserId).AsMaybe();
                    if (maybeGuildUser.HasNoValue)
                    {
                        // User not in guild, mark for deletion
                        guildMembersToDelete.Add(dbGuildMember.GuildMemberId);
                    }

                    // Rate limiting: small delay between checks
                    await Task.Delay(50);
                }

                // Delete in bulk
                if (guildMembersToDelete.Count > 0)
                {
                    await guildService.DeleteGuildMembersBulkAsync(guildMembersToDelete);
                    totalDeleted += guildMembersToDelete.Count;
                    Log.Information($"Deleted {guildMembersToDelete.Count} stale guild members in this batch");
                }

                skip += batchSize;

                // Longer delay between batches
                await Task.Delay(2000);
            }

            Log.Information($"Completed {nameof(CleanupStaleGuildMembers)}: Processed {totalProcessed} guild members, deleted {totalDeleted}");
        }
        catch (Exception e)
        {
            Log.Error(e, nameof(CleanupStaleGuildMembers));
            throw;
        }
    }

    /// <summary>
    /// Removes users from database who have no guild memberships.
    /// This should run after CleanupStaleGuildMembers to ensure orphaned users are removed.
    /// </summary>
    public async Task CleanupStaleUsers()
    {
        Log.Information($"Running {nameof(CleanupStaleUsers)}");

        try
        {
            await using var db = await contextFactory.CreateDbContextAsync();

            // Find users with no guild memberships
            var usersWithNoGuilds = await db.Users
                .Where(u => !u.GuildMembers.Any())
                .Select(u => u.UserId)
                .ToListAsync();

            if (usersWithNoGuilds.Count > 0)
            {
                Log.Information($"Found {usersWithNoGuilds.Count} users with no guild memberships");

                foreach (var userId in usersWithNoGuilds)
                {
                    await userService.DeleteUserAsync((ulong)userId);
                    await Task.Delay(100); // Rate limiting
                }

                Log.Information($"Completed {nameof(CleanupStaleUsers)}: Deleted {usersWithNoGuilds.Count} users");
            }
            else
            {
                Log.Information($"Completed {nameof(CleanupStaleUsers)}: No users to delete");
            }
        }
        catch (Exception e)
        {
            Log.Error(e, nameof(CleanupStaleUsers));
            throw;
        }
    }

    /// <summary>
    /// Syncs user data (username, avatar) from Discord for users who can still be resolved.
    /// Processes in batches to avoid memory issues and rate limits.
    /// </summary>
    public async Task SyncUserData()
    {
        Log.Information($"Running {nameof(SyncUserData)}");

        try
        {
            var clientGuilds = client.Guilds.AsMaybe();
            if (clientGuilds.HasNoValue)
            {
                Log.Information($"Client guilds not available, cancelling {nameof(SyncUserData)}");
                return;
            }

            const int batchSize = 50;
            var skip = 0;
            var totalProcessed = 0;
            var totalSynced = 0;

            while (true)
            {
                var users = await userService.GetUserBatchAsync(batchSize, skip);
                if (users.Count == 0) break;

                foreach (var dbUser in users)
                {
                    totalProcessed++;

                    try
                    {
                        // Try to resolve user from Discord
                        var maybeDiscordUser = (await userResolver.GetUserAsync((ulong)dbUser.UserId)).AsMaybe();
                        if (maybeDiscordUser.HasValue)
                        {
                            var synced = await userService.SyncUserFromDiscordAsync(dbUser, maybeDiscordUser.Value);
                            if (synced)
                            {
                                totalSynced++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, $"Failed to sync user {dbUser.UserId}");
                    }

                    // Rate limiting: delay between each user
                    await Task.Delay(200);
                }

                skip += batchSize;

                // Longer delay between batches
                await Task.Delay(3000);
            }

            Log.Information($"Completed {nameof(SyncUserData)}: Processed {totalProcessed} users, synced {totalSynced}");
        }
        catch (Exception e)
        {
            Log.Error(e, nameof(SyncUserData));
            throw;
        }
    }

    /// <summary>
    /// Syncs guild data (name, icon) from Discord.
    /// Processes in batches to avoid memory issues.
    /// </summary>
    public async Task SyncGuildData()
    {
        Log.Information($"Running {nameof(SyncGuildData)}");

        try
        {
            var clientGuilds = client.Guilds.AsMaybe();
            if (clientGuilds.HasNoValue)
            {
                Log.Information($"Client guilds not available, cancelling {nameof(SyncGuildData)}");
                return;
            }

            const int batchSize = 50;
            var skip = 0;
            var totalProcessed = 0;
            var totalSynced = 0;

            while (true)
            {
                var guilds = await guildService.GetGuildBatchAsync(batchSize, skip);
                if (guilds.Count == 0) break;

                foreach (var dbGuild in guilds)
                {
                    totalProcessed++;

                    try
                    {
                        var maybeDiscordGuild = client.GetGuild((ulong)dbGuild.GuildId).AsMaybe();
                        if (maybeDiscordGuild.HasValue)
                        {
                            var synced = await guildService.SyncGuildFromDiscordAsync(dbGuild, maybeDiscordGuild.Value);
                            if (synced)
                            {
                                totalSynced++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, $"Failed to sync guild {dbGuild.GuildId}");
                    }

                    // Rate limiting
                    await Task.Delay(100);
                }

                skip += batchSize;

                // Longer delay between batches
                await Task.Delay(2000);
            }

            Log.Information($"Completed {nameof(SyncGuildData)}: Processed {totalProcessed} guilds, synced {totalSynced}");
        }
        catch (Exception e)
        {
            Log.Error(e, nameof(SyncGuildData));
            throw;
        }
    }

    /// <summary>
    /// Syncs guild member data (avatar) from Discord.
    /// Processes in batches to avoid memory issues and rate limits.
    /// </summary>
    public async Task SyncGuildMemberData()
    {
        Log.Information($"Running {nameof(SyncGuildMemberData)}");

        try
        {
            var clientGuilds = client.Guilds.AsMaybe();
            if (clientGuilds.HasNoValue)
            {
                Log.Information($"Client guilds not available, cancelling {nameof(SyncGuildMemberData)}");
                return;
            }

            const int batchSize = 100;
            var skip = 0;
            var totalProcessed = 0;
            var totalSynced = 0;

            while (true)
            {
                var guildMembers = await guildService.GetGuildMemberBatchAsync(batchSize, skip);
                if (guildMembers.Count == 0) break;

                foreach (var dbGuildMember in guildMembers)
                {
                    totalProcessed++;

                    try
                    {
                        var maybeDiscordGuild = client.GetGuild((ulong)dbGuildMember.GuildId).AsMaybe();
                        if (maybeDiscordGuild.HasValue)
                        {
                            var maybeDiscordGuildUser = maybeDiscordGuild.Value.GetUser((ulong)dbGuildMember.UserId).AsMaybe();
                            if (maybeDiscordGuildUser.HasValue)
                            {
                                var synced = await guildService.SyncGuildMemberFromDiscordAsync(dbGuildMember, maybeDiscordGuildUser.Value);
                                if (synced)
                                {
                                    totalSynced++;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, $"Failed to sync guild member {dbGuildMember.GuildMemberId}");
                    }

                    // Rate limiting
                    await Task.Delay(50);
                }

                skip += batchSize;

                // Longer delay between batches
                await Task.Delay(2000);
            }

            Log.Information($"Completed {nameof(SyncGuildMemberData)}: Processed {totalProcessed} guild members, synced {totalSynced}");
        }
        catch (Exception e)
        {
            Log.Error(e, nameof(SyncGuildMemberData));
            throw;
        }
    }
}
