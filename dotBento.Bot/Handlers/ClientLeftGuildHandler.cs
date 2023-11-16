using Discord;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Models;
using dotBento.Bot.Services;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace dotBento.Bot.Handlers;

public class ClientLeftGuildHandler
{
    private readonly IMemoryCache _cache;
    private readonly DiscordSocketClient _client;
    private readonly GuildService _guildService;
    
    public ClientLeftGuildHandler(DiscordSocketClient client,
        GuildService guildService, IMemoryCache cache)
    {
        _cache = cache;
        _client = client;
        _guildService = guildService;
        _client.LeftGuild += ClientLeftGuildEvent;
    }
    
    private Task ClientLeftGuildEvent(SocketGuild guild)
    {
        _ = Task.Run(() => ClientLeftGuild(guild));
        return Task.CompletedTask;
    }
    
    private async Task ClientLeftGuild(SocketGuild guild)
    {
        var keepData = false;

        var key = $"{guild.Id}-keep-data";
        if (_cache.TryGetValue(key, out _))
        {
            keepData = true;
        }
        /*
         TODO
        if (BotTypeExtension.GetBotType(this._client.CurrentUser.Id) == BotType.Beta)
        {
            keepData = true;
        }
        */
        if (!keepData)
        {
            Log.Information(
                "LeftGuild: {guildName} / {guildId} | {memberCount} members", guild.Name, guild.Id, guild.MemberCount);
            await _guildService.RemoveGuildAsync(guild.Id);
        }
        else
        {
            Log.Information(
                "LeftGuild: {guildName} / {guildId} | {memberCount} members (skipped delete)", guild.Name, guild.Id, guild.MemberCount);
        }
    }
}