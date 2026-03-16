using System.Diagnostics;
using CSharpFunctionalExtensions;
using NetCord;
using NetCord.Gateway;
using dotBento.Bot.Models;
using dotBento.Domain;
using dotBento.EntityFramework.Context;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace dotBento.Bot.Services;

public sealed class BackgroundService(UserService userService,
    GuildService guildService,
    GatewayClient client,
    SupporterService supporterService,
    BotListService botListService,
    ReminderCommands reminderCommands,
    IDbContextFactory<BotDbContext> contextFactory,
    IDiscordUserResolver userResolver,
    IDmSender dmSender,
    IOptions<BotEnvConfig> botSettings)
{
    public void QueueJobs()
    {
        var isProduction = string.Equals(botSettings.Value.Environment, "production", StringComparison.OrdinalIgnoreCase);

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

        if (isProduction)
        {
            Log.Information($"RecurringJob: Adding {nameof(UpdateMetrics)}");
            RecurringJob.AddOrUpdate(nameof(UpdateMetrics), () => UpdateMetrics(), "* * * * *");

            Log.Information($"RecurringJob: Adding {nameof(UpdateBotLists)}");
            RecurringJob.AddOrUpdate(nameof(UpdateBotLists), () => UpdateBotLists(), "*/10 * * * *");

            Log.Information($"RecurringJob: Adding {nameof(CleanupStaleUsers)}");
            RecurringJob.AddOrUpdate(nameof(CleanupStaleUsers), () => CleanupStaleUsers(), "0 2 * * *");

            Log.Information($"RecurringJob: Adding {nameof(CleanupStaleGuilds)}");
            RecurringJob.AddOrUpdate(nameof(CleanupStaleGuilds), () => CleanupStaleGuilds(), "0 3 * * *");

            Log.Information($"RecurringJob: Adding {nameof(CleanupStaleGuildMembers)}");
            RecurringJob.AddOrUpdate(nameof(CleanupStaleGuildMembers), () => CleanupStaleGuildMembers(), "0 4 * * *");

            Log.Information($"RecurringJob: Adding {nameof(SyncUserData)}");
            RecurringJob.AddOrUpdate(nameof(SyncUserData), () => SyncUserData(), "0 5 * * *");

            Log.Information($"RecurringJob: Adding {nameof(SyncGuildData)}");
            RecurringJob.AddOrUpdate(nameof(SyncGuildData), () => SyncGuildData(), "0 6 * * *");

            Log.Information($"RecurringJob: Adding {nameof(SyncGuildMemberData)}");
            RecurringJob.AddOrUpdate(nameof(SyncGuildMemberData), () => SyncGuildMemberData(), "0 7 * * *");
        }
        else
        {
            RecurringJob.RemoveIfExists(nameof(UpdateMetrics));
            RecurringJob.RemoveIfExists(nameof(UpdateBotLists));
            RecurringJob.RemoveIfExists(nameof(CleanupStaleUsers));
            RecurringJob.RemoveIfExists(nameof(CleanupStaleGuilds));
            RecurringJob.RemoveIfExists(nameof(CleanupStaleGuildMembers));
            RecurringJob.RemoveIfExists(nameof(SyncUserData));
            RecurringJob.RemoveIfExists(nameof(SyncGuildData));
            RecurringJob.RemoveIfExists(nameof(SyncGuildMemberData));
        }
    }

    public async Task UpdateStatus()
    {
        Log.Information($"Running {nameof(UpdateStatus)}");
        var statusText = GetRandomActivityStatus(client);
        await client.UpdatePresenceAsync(new PresenceProperties(UserStatusType.Online)
            .WithActivities([new UserActivityProperties(statusText, UserActivityType.Watching)]));
    }

    private static string GetRandomActivityStatus(GatewayClient client)
    {
        var guildCount = client.Cache.Guilds.Count;
        var userCount = client.Cache.Guilds.Values.Sum(x => x.UserCount);

        var formattedUserCount = FormatThousandsCount(userCount);
        var formattedGuildCount = FormatThousandsCount(guildCount);

        return $"{formattedUserCount} {(userCount == 1 ? "user" : "users")} on {formattedGuildCount} {(guildCount == 1 ? "server" : "servers")}";
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
                var result = await dmSender.SendReminderAsync((ulong)reminder.UserId, reminder.Content);
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
        if (!string.Equals(botSettings.Value.Environment, "production", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Log.Information($"Running {nameof(UpdateMetrics)}");

        Statistics.RegisteredUserCount.Set(await userService.GetTotalDatabaseUserCountAsync());
        var discordUserCount = await userService.GetTotalDiscordUserCountAsync();
        Statistics.RegisteredDiscordUserCount.Set(discordUserCount.HasValue ? discordUserCount.Value : 0);
        Statistics.RegisteredGuildCount.Set(await guildService.GetTotalGuildCountAsync());
        Statistics.ActiveSupporterCount.Set(await supporterService.GetActiveSupporterCountAsync());

        try
        {
            if (client.Cache.Guilds.Count == 0)
            {
                Log.Information($"Client guild count is null, cancelling {nameof(UpdateMetrics)}");
                return;
            }

            var currentProcess = Process.GetCurrentProcess();
            var startTime = DateTime.Now - currentProcess.StartTime;

            if (startTime.Minutes > 8)
            {
                Statistics.DiscordServerCount.Set(client.Cache.Guilds.Count);
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
        Log.Information("Discord user cache clearing is not supported in NetCord");
    }

    public async Task UpdateGuildMemberCounts()
    {
        Log.Information($"Running {nameof(UpdateGuildMemberCounts)}");

        try
        {
            if (client.Cache.Guilds.Count == 0)
            {
                Log.Information($"Client guild count is null, cancelling {nameof(UpdateGuildMemberCounts)}");
                return;
            }

            foreach (var guild in client.Cache.Guilds.Values)
            {
                await guildService.UpdateGuildMemberCountAsync(guild.Id, guild.UserCount);
            }

            Log.Information($"Updated member counts for {client.Cache.Guilds.Count} guilds");
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
                    var discordUser = client.Cache.Guilds.Values
                        .Select(g => g.Users.GetValueOrDefault((ulong)user.UserId))
                        .FirstOrDefault(u => u is not null);
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
        await botListService.UpdateBotLists(client.Cache.Guilds.Count);
    }

    /// <summary>
    /// Removes guilds from database that the bot is no longer a member of.
    /// Cascade deletes all guild members for those guilds automatically.
    /// Runs at 3am daily, after CleanupStaleUsers.
    /// Processes in batches to avoid memory issues.
    /// </summary>
    public async Task CleanupStaleGuilds()
    {
        Log.Information($"Running {nameof(CleanupStaleGuilds)}");

        try
        {
            if (HasClientNoGuilds(nameof(CleanupStaleGuilds)))
            {
                return;
            }

            const int batchSize = 50;
            var skip = 0;
            var totalProcessed = 0;
            var totalDeleted = 0;

            while (true)
            {
                var dbGuilds = await guildService.GetGuildBatchAsync(batchSize, skip);
                if (dbGuilds.Count == 0) break;

                foreach (var dbGuild in dbGuilds)
                {
                    totalProcessed++;

                    var guild = client.Cache.Guilds.GetValueOrDefault((ulong)dbGuild.GuildId).AsMaybe();
                    if (guild.HasNoValue)
                    {
                        Log.Information($"Removing stale guild: {dbGuild.GuildName} ({dbGuild.GuildId})");
                        await guildService.RemoveGuildAsync((ulong)dbGuild.GuildId);
                        totalDeleted++;
                    }
                }

                skip += batchSize;
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
    /// Only handles members who left guilds that the bot is still in (other cases handled by cascade deletion).
    /// Uses REST API calls to verify membership (not cache) to avoid false positives.
    /// Runs at 4am daily, after CleanupStaleUsers and CleanupStaleGuilds.
    /// Processes in batches to avoid memory issues and rate limits.
    /// </summary>
    public async Task CleanupStaleGuildMembers()
    {
        Log.Information($"Running {nameof(CleanupStaleGuildMembers)}");

        try
        {
            if (HasClientNoGuilds(nameof(CleanupStaleGuildMembers)))
            {
                return;
            }

            const int batchSize = 100;
            var skip = 0;
            var totalProcessed = 0;
            var totalDeleted = 0;

            while (true)
            {
                var dbGuildMembers = await guildService.GetGuildMemberBatchAsync(batchSize, skip);
                if (dbGuildMembers.Count == 0) break;

                var guildMembersToDelete = new List<long>();

                foreach (var dbGuildMember in dbGuildMembers)
                {
                    totalProcessed++;

                    var guild = client.Cache.Guilds.GetValueOrDefault((ulong)dbGuildMember.GuildId).AsMaybe();
                    if (guild.HasNoValue)
                    {
                        // Guild no longer exists in bot's guild list - safe to delete
                        guildMembersToDelete.Add(dbGuildMember.GuildMemberId);
                        continue;
                    }

                    try
                    {
                        // Use REST API call instead of cache lookup to verify membership
                        // This ensures we don't delete valid members who just aren't in cache
                        var guildUser = await client.Rest.GetGuildUserAsync((ulong)dbGuildMember.GuildId, (ulong)dbGuildMember.UserId);
                        if (guildUser == null)
                        {
                            guildMembersToDelete.Add(dbGuildMember.GuildMemberId);
                        }
                    }
                    catch (NetCord.Rest.RestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // User not found in guild - safe to delete
                        guildMembersToDelete.Add(dbGuildMember.GuildMemberId);
                    }
                    catch (Exception ex)
                    {
                        // Log but don't delete on other errors - better to be safe
                        Log.Warning(ex, $"Failed to verify guild member {dbGuildMember.GuildMemberId} in guild {dbGuildMember.GuildId}");
                    }

                    // Rate-limit REST API calls to Discord
                    await Task.Delay(10000);
                }

                if (guildMembersToDelete.Count > 0)
                {
                    await guildService.DeleteGuildMembersBulkAsync(guildMembersToDelete);
                    totalDeleted += guildMembersToDelete.Count;
                    Log.Information($"Deleted {guildMembersToDelete.Count} stale guild members in this batch");
                }

                skip += batchSize;

                await Task.Delay(15000);
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
    /// Cascade deletes any stale guild member records for these users automatically.
    /// Runs at 2am daily, before CleanupStaleGuilds and CleanupStaleGuildMembers.
    /// Running first allows cascade deletions from subsequent jobs to handle most cleanup automatically.
    /// </summary>
    public async Task CleanupStaleUsers()
    {
        Log.Information($"Running {nameof(CleanupStaleUsers)}");

        try
        {
            var usersWithNoGuilds = await userService.GetUsersWithoutGuilds();

            if (usersWithNoGuilds.Count > 0)
            {
                Log.Information($"Found {usersWithNoGuilds.Count} users with no guild memberships");

                var deletedCount = 0;
                foreach (var userId in usersWithNoGuilds)
                {
                    try
                    {
                        await userService.DeleteUserAsync((ulong)userId);
                        deletedCount++;
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        Log.Warning($"User {userId} was already deleted (likely by cascade from guild member cleanup)");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Failed to delete user {userId}");
                    }

                    await Task.Delay(100);
                }

                Log.Information($"Completed {nameof(CleanupStaleUsers)}: Deleted {deletedCount} users ({usersWithNoGuilds.Count - deletedCount} already deleted)");
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
            if (HasClientNoGuilds(nameof(SyncUserData)))
            {
                return;
            }

            const int batchSize = 50;
            var skip = 0;
            var totalProcessed = 0;
            var totalSynced = 0;

            while (true)
            {
                var dbUsers = await userService.GetUserBatchAsync(batchSize, skip);
                if (dbUsers.Count == 0) break;

                foreach (var dbUser in dbUsers)
                {
                    totalProcessed++;

                    try
                    {
                        var discordUser = (await userResolver.GetUserAsync((ulong)dbUser.UserId)).AsMaybe();
                        if (discordUser.HasValue)
                        {
                            var synced = await userService.SyncUserFromDiscordAsync(dbUser, discordUser.Value);
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

                    // Rate-limit REST API calls to Discord
                    await Task.Delay(10000);
                }

                skip += batchSize;

                await Task.Delay(15000);
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
            if (HasClientNoGuilds(nameof(SyncGuildData)))
            {
                return;
            }

            const int batchSize = 50;
            var skip = 0;
            var totalProcessed = 0;
            var totalSynced = 0;

            while (true)
            {
                var dbGuilds = await guildService.GetGuildBatchAsync(batchSize, skip);
                if (dbGuilds.Count == 0) break;

                foreach (var dbGuild in dbGuilds)
                {
                    totalProcessed++;

                    try
                    {
                        var discordGuild = client.Cache.Guilds.GetValueOrDefault((ulong)dbGuild.GuildId).AsMaybe();
                        if (discordGuild.HasValue)
                        {
                            var synced = await guildService.SyncGuildFromDiscordAsync(dbGuild, discordGuild.Value);
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
                }

                skip += batchSize;
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
    /// Uses REST API calls to fetch members (not cache) to ensure all members can be synced.
    /// Processes in batches to avoid memory issues and rate limits.
    /// </summary>
    public async Task SyncGuildMemberData()
    {
        Log.Information($"Running {nameof(SyncGuildMemberData)}");

        try
        {
            if (HasClientNoGuilds(nameof(SyncGuildMemberData)))
            {
                return;
            }

            const int batchSize = 100;
            var skip = 0;
            var totalProcessed = 0;
            var totalSynced = 0;

            while (true)
            {
                var dbGuildMembers = await guildService.GetGuildMemberBatchAsync(batchSize, skip);
                if (dbGuildMembers.Count == 0) break;

                foreach (var dbGuildMember in dbGuildMembers)
                {
                    totalProcessed++;

                    try
                    {
                        var discordGuild = client.Cache.Guilds.GetValueOrDefault((ulong)dbGuildMember.GuildId).AsMaybe();
                        if (discordGuild.HasValue)
                        {
                            // Use REST API call instead of cache lookup
                            var discordGuildUser = await client.Rest.GetGuildUserAsync((ulong)dbGuildMember.GuildId, (ulong)dbGuildMember.UserId);
                            if (discordGuildUser != null)
                            {
                                var synced = await guildService.SyncGuildMemberFromDiscordAsync(dbGuildMember, discordGuildUser);
                                if (synced)
                                {
                                    totalSynced++;
                                }
                            }
                        }
                    }
                    catch (NetCord.Rest.RestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Member no longer in guild — remove stale record
                        await guildService.DeleteGuildMember((ulong)dbGuildMember.GuildId, (ulong)dbGuildMember.UserId);
                        Log.Information($"Removed stale guild member {dbGuildMember.UserId} from guild {dbGuildMember.GuildId} during sync");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, $"Failed to sync guild member {dbGuildMember.GuildMemberId}");
                    }

                    // Rate-limit REST API calls to Discord
                    await Task.Delay(10000);
                }

                skip += batchSize;

                await Task.Delay(15000);
            }

            Log.Information($"Completed {nameof(SyncGuildMemberData)}: Processed {totalProcessed} guild members, synced {totalSynced}");
        }
        catch (Exception e)
        {
            Log.Error(e, nameof(SyncGuildMemberData));
            throw;
        }
    }

    private bool HasClientNoGuilds(string jobName)
    {
        if (client.Cache.Guilds.Count == 0)
        {
            Log.Information($"Client guilds not available, cancelling {jobName}");
            return true;
        }

        return false;
    }
}
