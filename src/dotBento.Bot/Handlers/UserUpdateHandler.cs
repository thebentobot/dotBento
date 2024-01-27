using Discord.WebSocket;
using dotBento.Domain;
using dotBento.Infrastructure.Services;

namespace dotBento.Bot.Handlers;

public class UserUpdateHandler
{
    private readonly DiscordSocketClient _client;
    private readonly UserService _userService;
    
    public UserUpdateHandler(DiscordSocketClient client, UserService userService)
    {
        _userService = userService;
        _client = client;
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