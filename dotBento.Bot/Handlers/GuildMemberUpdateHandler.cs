using Discord;
using Discord.WebSocket;
using dotBento.Bot.Services;
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
        var oldGuildUser = cacheable.Value;
        if (oldGuildUser.GetGuildAvatarUrl() != newGuildUser.GetGuildAvatarUrl())
        {
            
        }
    }
}