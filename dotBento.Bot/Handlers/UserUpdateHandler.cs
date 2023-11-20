using Discord;
using Discord.WebSocket;
using dotBento.Bot.Services;
using dotBento.Domain;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Bot.Handlers;

public class UserUpdateHandler
{
    private readonly IMemoryCache _cache;
    private readonly DiscordSocketClient _client;
    private readonly UserService _userService;
    
    public UserUpdateHandler(DiscordSocketClient client, IMemoryCache cache, UserService userService)
    {
        _userService = userService;
        _client = client;
        _cache = cache;
        _client.UserUpdated += UserUpdateEvent;
    }

    private Task UserUpdateEvent(SocketUser socketUser, SocketUser newUser)
    {
        _ = Task.Run(() => UserUpdated(socketUser, newUser));
        return Task.CompletedTask;
    }
    
    private async Task UserUpdated(SocketUser oldUser, SocketUser newUser)
    {
        if (newUser.IsBot) return;
        var getUserFromDatabaseAsync = await _userService.GetUserFromDatabaseAsync(newUser.Id);
        if (getUserFromDatabaseAsync.HasNoValue) return;
        if (oldUser.GetAvatarUrl() != newUser.GetAvatarUrl())
        {
            Statistics.DiscordEvents.WithLabels(nameof(UserUpdated)).Inc();
            await _userService.UpdateUserAvatarAsync(newUser);
        }
        if (oldUser.Username != newUser.Username)
        {
            Statistics.DiscordEvents.WithLabels(nameof(UserUpdated)).Inc();
            await _userService.UpdateUserUsernameAsync(newUser);
        }
    }
}