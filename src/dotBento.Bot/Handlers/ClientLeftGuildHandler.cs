using dotBento.Domain;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using NetCord.Gateway;
using Serilog;

namespace dotBento.Bot.Handlers;

public sealed class ClientLeftGuildHandler : IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly GatewayClient _client;
    private readonly GuildService _guildService;

    public ClientLeftGuildHandler(GatewayClient client,
        GuildService guildService, IMemoryCache cache)
    {
        _cache = cache;
        _client = client;
        _guildService = guildService;
        _client.GuildDelete += ClientLeftGuildEvent;
    }

    private ValueTask ClientLeftGuildEvent(GuildDeleteEventArgs args)
    {
        _ = Task.Run(async () =>
        {
            try { await ClientLeftGuild(args.GuildId); }
            catch (Exception ex) { Log.Error(ex, "Unhandled exception in ClientLeftGuild handler"); }
        });
        return ValueTask.CompletedTask;
    }

    private async Task ClientLeftGuild(ulong guildId)
    {
        var keepData = false;

        var key = $"{guildId}-keep-data";
        if (_cache.TryGetValue(key, out _))
        {
            keepData = true;
        }
        if (_client.Cache.User?.Id == Constants.BotDevelopmentId)
        {
            keepData = true;
        }
        if (!keepData)
        {
            Statistics.DiscordEvents.WithLabels(nameof(ClientLeftGuild)).Inc();
            Log.Information("LeftGuild: {GuildId}", guildId);
            await _guildService.RemoveGuildAsync(guildId);
        }
        else
        {
            Log.Information("LeftGuild: {GuildId} (skipped delete)", guildId);
        }
    }

    public void Dispose()
    {
        _client.GuildDelete -= ClientLeftGuildEvent;
    }
}
