using dotBento.Bot.Services;
using dotBento.Domain;
using dotBento.Infrastructure.Services;
using NetCord;
using NetCord.Gateway;
using Serilog;

namespace dotBento.Bot.Handlers;

public sealed class GuildMemberUpdateHandler : IDisposable
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
        _ = Task.Run(async () =>
        {
            try { await GuildUserUpdated(member); }
            catch (Exception ex) { Log.Error(ex, "Unhandled exception in GuildUserUpdated handler"); }
        });
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

    public void Dispose()
    {
        _client.GuildUserUpdate -= GuildUserUpdateEvent;
    }
}
