using Discord;
using Discord.WebSocket;
using dotBento.Bot.Services;
using dotBento.Domain;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Handlers;

public class UserEventHandler
{
    private readonly DiscordSocketClient _client;
    private readonly UserService _userService;

    public UserEventHandler(DiscordSocketClient client, UserService userService)
    {
        this._client = client;
        this._userService = userService;
        this._client.UserJoined += UserJoined;
        this._client.UserLeft += UserLeft;
        this._client.UserBanned += UserBanned;
        this._client.GuildMemberUpdated += GuildMemberUpdated;
    }

    private async Task GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> cacheable, SocketGuildUser newGuildUser)
    {
        Statistics.DiscordEvents.WithLabels(nameof(GuildMemberUpdated)).Inc();
    }

    private async Task UserJoined(SocketGuildUser socketGuildUser)
    {
        Statistics.DiscordEvents.WithLabels(nameof(UserJoined)).Inc();
    }

    private async Task UserLeft(SocketGuild socketGuild, SocketUser socketUser)
    {
        Statistics.DiscordEvents.WithLabels(nameof(UserLeft)).Inc();
    }

    private async Task UserBanned(SocketUser guildUser, SocketGuild guild)
    {
        Statistics.DiscordEvents.WithLabels(nameof(UserBanned)).Inc();
    }
}