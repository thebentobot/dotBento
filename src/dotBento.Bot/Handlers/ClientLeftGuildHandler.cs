using Discord.WebSocket;
using dotBento.Domain;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace dotBento.Bot.Handlers;

public sealed class ClientLeftGuildHandler : IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly dotBento.Bot.Models.BotEnvConfig _config;
    private readonly DiscordSocketClient _client;
    private readonly GuildService _guildService;

    public ClientLeftGuildHandler(DiscordSocketClient client,
        GuildService guildService, IMemoryCache cache,
        Microsoft.Extensions.Options.IOptions<dotBento.Bot.Models.BotEnvConfig>? config = null)
    {
        _cache = cache;
        _config = config?.Value ?? new();
        _client = client;
        _guildService = guildService;
        _client.LeftGuild += ClientLeftGuildEvent;
    }

    private Task ClientLeftGuildEvent(SocketGuild guild)
    {
        _ = Task.Run(async () =>
        {
            try { await ClientLeftGuild(guild); }
            catch (Exception ex) { Log.Error(ex, "Unhandled exception in ClientLeftGuild handler"); }
        });
        return Task.CompletedTask;
    }

    private async Task ClientLeftGuild(SocketGuild guild)
    {
        var keepData = false;

        var key = $"{guild.Id}-keep-data";
        if (_cache.TryGetValue(key, out _))
        {
            keepData = true;
        }
        if (_config.KeepGuildDataOnLeave)
        {
            keepData = true;
        }
        if (!keepData)
        {
            Statistics.DiscordEvents.WithLabels(nameof(ClientLeftGuild)).Inc();
            Log.Information(
                "LeftGuild: {GuildName} / {GuildId} | {MemberCount} members", guild.Name, guild.Id, guild.MemberCount);
            await _guildService.RemoveGuildAsync(guild.Id);
        }
        else
        {
            Log.Information(
                "LeftGuild: {GuildName} / {GuildId} | {MemberCount} members (skipped delete)", guild.Name, guild.Id, guild.MemberCount);
        }
    }

    public void Dispose()
    {
        _client.LeftGuild -= ClientLeftGuildEvent;
    }
}
