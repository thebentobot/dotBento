using Discord.WebSocket;
using dotBento.Domain;
using dotBento.Infrastructure.Services;
using Serilog;

namespace dotBento.Bot.Handlers;

public sealed class GuildMemberRemoveHandler : IDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly GuildService _guildService;
    private readonly UserService _userService;

    public GuildMemberRemoveHandler(DiscordSocketClient client,
        GuildService guildService, UserService userService)
    {
        _userService = userService;
        _client = client;
        _guildService = guildService;
        _client.UserLeft += GuildMemberRemovedEvent;
        _client.UserBanned += GuildMemberBannedEvent;
    }

    private Task GuildMemberRemovedEvent(SocketGuild guild, SocketUser user)
    {
        _ = Task.Run(async () =>
        {
            try { await GuildMemberRemoved(guild, user); }
            catch (Exception ex) { Log.Error(ex, "Unhandled exception in GuildMemberRemoved handler"); }
        });
        return Task.CompletedTask;
    }

    private Task GuildMemberBannedEvent(SocketUser user, SocketGuild guild)
    {
        _ = Task.Run(async () =>
        {
            try { await GuildMemberRemoved(guild, user); }
            catch (Exception ex) { Log.Error(ex, "Unhandled exception in GuildMemberBanned handler"); }
        });
        return Task.CompletedTask;
    }

    private async Task GuildMemberRemoved(SocketGuild guild, SocketUser discordUser)
    {
        if (discordUser.IsBot) return;

        Statistics.DiscordEvents.WithLabels(nameof(GuildMemberRemoved)).Inc();
        await _guildService.DeleteGuildMember(guild.Id, discordUser.Id);
        var guildsForUser = _guildService.FindGuildsForUser(discordUser.Id).Result;
        if (guildsForUser.Count == 0)
        {
            await _userService.DeleteUserAsync(discordUser.Id);
        }
    }

    public void Dispose()
    {
        _client.UserLeft -= GuildMemberRemovedEvent;
        _client.UserBanned -= GuildMemberBannedEvent;
    }
}
