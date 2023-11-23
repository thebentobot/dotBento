using Discord.WebSocket;
using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Bot.Services;

public class SupporterService(IDbContextFactory<BotDbContext> contextFactory,
    IMemoryCache cache,
    DiscordSocketClient client)
{
    private readonly IMemoryCache _cache = cache;
    private readonly DiscordSocketClient _client = client;


    public async Task<int> GetActiveSupporterCountAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Patreons
            .AsQueryable()
            .CountAsync();
    }
}