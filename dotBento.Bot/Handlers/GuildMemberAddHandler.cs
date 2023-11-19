using Discord.WebSocket;
using dotBento.Bot.Services;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Bot.Handlers;

public class GuildMemberAddHandler
{
    private readonly IMemoryCache _cache;
    private readonly DiscordSocketClient _client;
    private readonly GuildService _guildService;
    private readonly UserService _userService;
    
    public GuildMemberAddHandler(DiscordSocketClient client,
        GuildService guildService, IMemoryCache cache, UserService userService)
    {
        _cache = cache;
        _userService = userService;
        _client = client;
        _guildService = guildService;
        _client.UserJoined += GuildMemberAddedEvent;
    }
    
    private Task GuildMemberAddedEvent(SocketGuildUser user)
    {
        _ = Task.Run(() => GuildMemberAdded(user));
        return Task.CompletedTask;
    }
    
    private async Task GuildMemberAdded(SocketGuildUser discordUser)
    {
        if (discordUser.IsBot) return;
    }
}