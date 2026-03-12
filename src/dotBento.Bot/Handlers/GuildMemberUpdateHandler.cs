using dotBento.Bot.Services;
using dotBento.Domain;
using dotBento.Infrastructure.Services;
using NetCord;
using NetCord.Gateway;

namespace dotBento.Bot.Handlers;

public sealed class GuildMemberUpdateHandler
{
    private readonly GatewayClient _client;
    private readonly GuildService _guildService;
    private readonly GuildMemberLookupService _memberLookup;

    public GuildMemberUpdateHandler(GatewayClient client,
        GuildService guildService, GuildMemberLookupService memberLookup)
    {
        _client = client;
        _guildService = guildService;
        _memberLookup = memberLookup;
        _client.GuildUserUpdate += GuildUserUpdateEvent;
    }

    private ValueTask GuildUserUpdateEvent(GuildUser member)
    {
        _ = Task.Run(() => GuildUserUpdated(member));
        return ValueTask.CompletedTask;
    }

    private async Task GuildUserUpdated(GuildUser member)
    {
        if (member.IsBot) return;
        _memberLookup.Update(member);
        var getGuildMemberFromDatabaseAsync = await _guildService.GetGuildMemberAsync(member.GuildId, member.Id);
        if (getGuildMemberFromDatabaseAsync.HasValue)
        {
            Statistics.DiscordEvents.WithLabels(nameof(GuildUserUpdated)).Inc();
            await _guildService.UpdateGuildMemberAvatarAsync(member);
        }
    }
}
