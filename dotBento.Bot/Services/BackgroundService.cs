using System.Diagnostics;
using Discord.WebSocket;
using dotBento.Domain;
using Hangfire;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace dotBento.Bot.Services;

public class BackgroundService
{
    private readonly UserService _userService;
    private readonly GuildService _guildService;
    private readonly DiscordSocketClient _client;
    private readonly IMemoryCache _cache;
    private readonly SupporterService _supporterService;

    public BackgroundService(UserService userService,
        GuildService guildService,
        DiscordSocketClient client,
        IMemoryCache cache,
        SupporterService supporterService)
    {
        _userService = userService;
        _guildService = guildService;
        _client = client;
        _cache = cache;
        _supporterService = supporterService;
    }

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

        Statistics.RegisteredUserCount.Set(await _userService.GetTotalDatabaseUserCountAsync());
        Statistics.RegisteredDiscordUserCount.Set(await _userService.GetTotalDiscordUserCountAsync());
        Statistics.RegisteredGuildCount.Set(await _guildService.GetTotalGuildCountAsync());
        Statistics.ActiveSupporterCount.Set(await _supporterService.GetActiveSupporterCountAsync());

        try
        {
            if (_client?.Guilds?.Count == null)
            {
                Log.Information($"Client guild count is null, cancelling {nameof(UpdateMetrics)}");
                return;
            }

            var currentProcess = Process.GetCurrentProcess();
            var startTime = DateTime.Now - currentProcess.StartTime;

            if (startTime.Minutes > 8)
            {
                Statistics.DiscordServerCount.Set(_client.Guilds.Count);
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
        _client.PurgeUserCache();
        Log.Information("Purged discord user cache");
    }
}