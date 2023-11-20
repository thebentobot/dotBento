using Discord.WebSocket;
using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Bot.Services;

public class SupporterService
{
    private readonly IDbContextFactory<BotDbContext> _contextFactory;
    private readonly IMemoryCache _cache;
    private readonly DiscordSocketClient _client;

    public SupporterService(IDbContextFactory<BotDbContext> contextFactory,
        IMemoryCache cache,
        DiscordSocketClient client)
    {
        _contextFactory = contextFactory;
        _cache = cache;
        _client = client;
    }


    public async Task<int> GetActiveSupporterCountAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.Patreons
            .AsQueryable()
            .CountAsync();
    }
}