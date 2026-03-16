using dotBento.Domain;
using dotBento.Infrastructure.Services;
using NetCord;
using NetCord.Gateway;
using Serilog;

namespace dotBento.Bot.Handlers;

public sealed class UserUpdateHandler : IDisposable
{
    private readonly GatewayClient _client;
    private readonly UserService _userService;

    public UserUpdateHandler(GatewayClient client, UserService userService)
    {
        _userService = userService;
        _client = client;
        _client.GuildUserUpdate += UserUpdateEvent;
    }

    private ValueTask UserUpdateEvent(GuildUser user)
    {
        _ = Task.Run(async () =>
        {
            try { await UserUpdated(user); }
            catch (Exception ex) { Log.Error(ex, "Unhandled exception in UserUpdated handler"); }
        });
        return ValueTask.CompletedTask;
    }

    private async Task UserUpdated(GuildUser user)
    {
        if (user.IsBot) return;
        var getUserFromDatabaseAsync = await _userService.GetUserFromDatabaseAsync(user.Id);
        if (getUserFromDatabaseAsync.HasNoValue) return;

        var dbUser = getUserFromDatabaseAsync.Value;
        var currentAvatarUrl = user.AvatarHash != null
            ? $"https://cdn.discordapp.com/avatars/{user.Id}/{user.AvatarHash}.png?size=1024"
            : null;

        if (dbUser.AvatarUrl != currentAvatarUrl)
        {
            Statistics.DiscordEvents.WithLabels(nameof(UserUpdated)).Inc();
            await _userService.UpdateUserAvatarAsync(user);
        }
        if (dbUser.Username != user.Username)
        {
            Statistics.DiscordEvents.WithLabels(nameof(UserUpdated)).Inc();
            await _userService.UpdateUserUsernameAsync(user);
        }
    }

    public void Dispose()
    {
        _client.GuildUserUpdate -= UserUpdateEvent;
    }
}
