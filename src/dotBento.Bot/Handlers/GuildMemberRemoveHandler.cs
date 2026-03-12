using dotBento.Bot.Services;
using dotBento.Domain;
using dotBento.Infrastructure.Services;
using NetCord.Gateway;

namespace dotBento.Bot.Handlers;

public sealed class GuildMemberRemoveHandler
{
    private readonly GatewayClient _client;
    private readonly GuildService _guildService;
    private readonly UserService _userService;
    private readonly GuildMemberLookupService _memberLookup;

    public GuildMemberRemoveHandler(GatewayClient client,
        GuildService guildService, UserService userService,
        GuildMemberLookupService memberLookup)
    {
        _userService = userService;
        _client = client;
        _guildService = guildService;
        _memberLookup = memberLookup;
        _client.GuildUserRemove += GuildMemberRemovedEvent;
        _client.GuildBanAdd += GuildMemberBannedEvent;
    }

    private ValueTask GuildMemberRemovedEvent(GuildUserRemoveEventArgs args)
    {
        _ = Task.Run(() => GuildMemberRemoved(args.GuildId, args.User.Id));
        return ValueTask.CompletedTask;
    }

    private ValueTask GuildMemberBannedEvent(GuildBanEventArgs args)
    {
        _ = Task.Run(() => GuildMemberRemoved(args.GuildId, args.User.Id));
        return ValueTask.CompletedTask;
    }

    private async Task GuildMemberRemoved(ulong guildId, ulong userId)
    {
        Statistics.DiscordEvents.WithLabels(nameof(GuildMemberRemoved)).Inc();
        _memberLookup.Invalidate(guildId, userId);
        await _guildService.DeleteGuildMember(guildId, userId);
        var guildsForUser = await _guildService.FindGuildsForUser(userId);
        if (guildsForUser.Count == 0)
        {
            await _userService.DeleteUserAsync(userId);
        }
    }
}
