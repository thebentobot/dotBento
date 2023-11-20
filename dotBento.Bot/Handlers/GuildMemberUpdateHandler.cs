using Discord;
using Discord.WebSocket;
using dotBento.Bot.Services;
using dotBento.Domain;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Bot.Handlers;

public class GuildMemberUpdateHandler
{
    private readonly IMemoryCache _cache;
    private readonly DiscordSocketClient _client;
    private readonly GuildService _guildService;
    private readonly UserService _userService;
    
    public GuildMemberUpdateHandler(DiscordSocketClient client,
        GuildService guildService, IMemoryCache cache, UserService userService)
    {
        _userService = userService;
        _client = client;
        _guildService = guildService;
        _cache = cache;
        _client.GuildMemberUpdated += GuildMemberUpdateEvent;
    }

    private Task GuildMemberUpdateEvent(Cacheable<SocketGuildUser, ulong> cacheable, SocketGuildUser newGuildUser)
    {
        _ = Task.Run(() => GuildMemberUpdated(cacheable, newGuildUser));
        return Task.CompletedTask;
    }
    
    private async Task GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> cacheable, SocketGuildUser newGuildUser)
    {
        if (newGuildUser.IsBot) return;
        var getGuildMemberFromDatabaseAsync = await _guildService.GetGuildMemberAsync(newGuildUser.Guild.Id, newGuildUser.Id);
        var oldGuildUser = cacheable.Value;
        if (getGuildMemberFromDatabaseAsync.HasValue && oldGuildUser.GetGuildAvatarUrl() != newGuildUser.GetGuildAvatarUrl())
        {
            Statistics.DiscordEvents.WithLabels(nameof(GuildMemberUpdated)).Inc();
            await _guildService.UpdateGuildMemberAvatarAsync(newGuildUser);
        }
    }
}