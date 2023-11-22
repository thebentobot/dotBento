using System.Diagnostics;
using Discord.WebSocket;
using dotBento.Domain;
using Hangfire;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace dotBento.Bot.Services;

public class BackgroundService(UserService userService,
    GuildService guildService,
    DiscordSocketClient client,
    SupporterService supporterService)
{
    public void QueueJobs()
    {
        Log.Information($"RecurringJob: Adding {nameof(UpdateMetrics)}");
        RecurringJob.AddOrUpdate(nameof(UpdateMetrics), () => UpdateMetrics(), "* * * * *");
        
        Log.Information($"RecurringJob: Adding {nameof(ClearUserCache)}");
        RecurringJob.AddOrUpdate(nameof(ClearUserCache), () => ClearUserCache(), "30 */2 * * *");
    }
    
    public async Task UpdateMetrics()
    {
        Log.Information($"Running {nameof(UpdateMetrics)}");

        Statistics.RegisteredUserCount.Set(await userService.GetTotalDatabaseUserCountAsync());
        Statistics.RegisteredDiscordUserCount.Set(await userService.GetTotalDiscordUserCountAsync());
        Statistics.RegisteredGuildCount.Set(await guildService.GetTotalGuildCountAsync());
        Statistics.ActiveSupporterCount.Set(await supporterService.GetActiveSupporterCountAsync());

        try
        {
            if (client?.Guilds?.Count == null)
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