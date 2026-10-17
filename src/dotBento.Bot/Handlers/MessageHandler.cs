using Discord.WebSocket;
using dotBento.Domain;
using dotBento.Infrastructure.Services;

namespace dotBento.Bot.Handlers;

public sealed class MessageHandler : IDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly UserService _userService;
    private readonly GuildService _guildService;

    public MessageHandler(DiscordSocketClient client, UserService userService, GuildService guildService)
    {
        _client = client;
        _userService = userService;
        _guildService = guildService;
        _client.MessageReceived += MessageReceived;
    }

    private async Task MessageReceived(SocketMessage message)
    {
        Statistics.DiscordEvents.WithLabels(nameof(MessageReceived)).Inc();

        if (message is not SocketUserMessage { Author: SocketGuildUser user } || user.IsBot)
        {
            return;
        }

        await _guildService.AddGuildAsync(user.Guild);
        await _userService.CreateOrAddUserToCache(user);
        await _guildService.AddGuildMemberAsync(user);

        var patreonUser = await _userService.GetPatreonUserAsync(user.Id);
        await _userService.AddExperienceAsync(user.Id, user.Guild.Id, patreonUser);
    }

    public void Dispose()
    {
        _client.MessageReceived -= MessageReceived;
    }
}
