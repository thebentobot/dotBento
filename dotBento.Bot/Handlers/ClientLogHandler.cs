using Discord;
using Discord.WebSocket;
using dotBento.Bot.Services;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using Serilog.Events;

namespace dotBento.Bot.Handlers;

public class ClientLogHandler
{
    private readonly IMemoryCache _cache;
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly GuildService _guildService;
    
    public ClientLogHandler(DiscordSocketClient client, IServiceProvider services, ILogger logger,
        GuildService guildService, IMemoryCache cache)
    {
        _client = client;
        _guildService = guildService;
        _cache = cache;
        _client.Log += LogEvent;
        _client.JoinedGuild += ClientJoinedGuildEvent;
        _client.LeftGuild += ClientLeftGuildEvent;
    }
    
    private Task ClientJoinedGuildEvent(SocketGuild guild)
    {
        _ = Task.Run(() => ClientJoinedGuild(guild));
        return Task.CompletedTask;
    }

    private Task ClientLeftGuildEvent(SocketGuild guild)
    {
        _ = Task.Run(() => ClientLeftGuild(guild));
        return Task.CompletedTask;
    }

    private Task LogEvent(LogMessage logMessage)
    {
        Task.Run(() =>
        {
            switch (logMessage.Severity)
            {
                case LogSeverity.Critical:
                    Log.Fatal(logMessage.Exception, "{logMessageSource} | {logMessage}", logMessage.Source, logMessage.Message);
                    break;
                case LogSeverity.Error:
                    Log.Error(logMessage.Exception, "{logMessageSource} | {logMessage}", logMessage.Source, logMessage.Message);
                    break;
                case LogSeverity.Warning:
                    Log.Warning(logMessage.Exception, "{logMessageSource} | {logMessage}", logMessage.Source, logMessage.Message);
                    break;
                case LogSeverity.Info:
                    Log.Information(logMessage.Exception, "{logMessageSource} | {logMessage}", logMessage.Source, logMessage.Message);
                    break;
                case LogSeverity.Verbose:
                    Log.Verbose(logMessage.Exception, "{logMessageSource} | {logMessage}", logMessage.Source, logMessage.Message);
                    break;
                case LogSeverity.Debug:
                    Log.Debug(logMessage.Exception, "{logMessageSource} | {logMessage}", logMessage.Source, logMessage.Message);
                    break;
            }

        });
        return Task.CompletedTask;
    }
    
    private async Task ClientJoinedGuild(SocketGuild guild)
    {
        Log.Information(
            "JoinedGuild: {guildName} / {guildId} | {memberCount} members", guild.Name, guild.Id, guild.MemberCount);
        
        await this._guildService.AddGuildAsync(guild.Id);
    }
    
    private async Task ClientLeftGuild(SocketGuild guild)
    {
        var keepData = false;

        var key = $"{guild.Id}-keep-data";
        if (this._cache.TryGetValue(key, out _))
        {
            keepData = true;
        }
        /*
        if (BotTypeExtension.GetBotType(this._client.CurrentUser.Id) == BotType.Beta)
        {
            keepData = true;
        }
        */
        if (!keepData)
        {
            Log.Information(
                "LeftGuild: {guildName} / {guildId} | {memberCount} members", guild.Name, guild.Id, guild.MemberCount);
            _ = this._guildService.RemoveGuildAsync(guild.Id);
        }
        else
        {
            Log.Information(
                "LeftGuild: {guildName} / {guildId} | {memberCount} members (skipped delete)", guild.Name, guild.Id, guild.MemberCount);
        }
    }
}