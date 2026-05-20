using Discord.WebSocket;
using dotBento.Domain;
using dotBento.Infrastructure.Services;
using Serilog;

namespace dotBento.Bot.Handlers;

public sealed class UserUpdateHandler : IDisposable
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
        _ = Task.Run(async () =>
        {
            try { await UserUpdated(socketUser, newUser); }
            catch (Exception ex) { Log.Error(ex, "Unhandled exception in UserUpdated handler"); }
        });
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

    public void Dispose()
    {
        _client.UserUpdated -= UserUpdateEvent;
    }
}
