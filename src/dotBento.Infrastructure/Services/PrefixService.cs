using System.Collections.Concurrent;
using dotBento.Domain;
using dotBento.EntityFramework.Context;
using dotBento.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dotBento.Infrastructure.Services;

public sealed class PrefixService(IDbContextFactory<BotDbContext> contextFactory) : IPrefixService
{
    private static readonly ConcurrentDictionary<ulong, string> ServerPrefixes = new();

    public void StorePrefix(string prefix, ulong key)
    {
        ServerPrefixes.AddOrUpdate(key, prefix, (_, _) => prefix);
    }


    public string GetPrefix(ulong? key)
    {
        var standardPrefix = Constants.StartPrefix;
        if (!key.HasValue)
        {
            return standardPrefix;
        }

        return !ServerPrefixes.ContainsKey(key.Value) ? standardPrefix : ServerPrefixes[key.Value];
    }


    public void RemovePrefix(ulong key)
    {
        ServerPrefixes.TryRemove(key, out _);
    }


    public async Task LoadAllPrefixes()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var servers = await db.Guilds.AsNoTracking().ToListAsync();
        foreach (var server in servers)
        {
            StorePrefix(server.Prefix, (ulong)server.GuildId);
        }
    }

    public async Task ReloadPrefix(ulong discordGuildId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var server = await db.Guilds
            .Where(w => w.GuildId == (long)discordGuildId)
            .FirstOrDefaultAsync();

        if (server == null)
        {
            RemovePrefix(discordGuildId);
        }
        else
        {
            StorePrefix(server.Prefix, (ulong)server.GuildId);
        }
    }
}
