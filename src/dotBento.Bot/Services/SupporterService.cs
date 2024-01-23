using CSharpFunctionalExtensions;
using Discord.WebSocket;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
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
    
    public async Task<Maybe<Patreon>> GetPatreonAsync(long userId)
    {
       if (_cache.TryGetValue<Patreon>(userId, out var patreon))
       {
           return patreon;
       }

       await using var db = await contextFactory.CreateDbContextAsync();
       patreon = await db.Patreons.FirstOrDefaultAsync(x => x.UserId == userId);
       if (patreon == null)
       {
           return Maybe<Patreon>.None;
       }

       _cache.Set(userId, patreon, TimeSpan.FromMinutes(5));
       return patreon.AsMaybe();
    }
}