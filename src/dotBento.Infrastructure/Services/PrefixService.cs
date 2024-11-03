using System.Collections.Concurrent;
using dotBento.Domain;
using dotBento.EntityFramework.Context;
using dotBento.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace dotBento.Infrastructure.Services;

public sealed class PrefixService(IDbContextFactory<BotDbContext> contextFactory) : IPrefixService
{
    private static readonly ConcurrentDictionary<ulong, string> ServerPrefixes = new();

    public void StorePrefix(string prefix, ulong key)
    {
        if (ServerPrefixes.ContainsKey(key))
        {
            var oldPrefix = GetPrefix(key);
            if (!ServerPrefixes.TryUpdate(key, prefix, oldPrefix))
            {
                Log.Warning("Failed to update custom prefix {Prefix} with the key: {Key} from the dictionary", prefix, key);
            }

            return;
        }

        if (!ServerPrefixes.TryAdd(key, prefix))
        {
            Log.Warning("Failed to add custom prefix {Prefix} with the key: {Key} from the dictionary", prefix, key);
        }
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
        if (!ServerPrefixes.ContainsKey(key))
        {
            return;
        }

        if (!ServerPrefixes.TryRemove(key, out var removedPrefix))
        {
            Log.Warning("Failed to remove custom prefix {RemovedPrefix} with the key: {Key} from the dictionary", removedPrefix, key);
        }
    }


    public async Task LoadAllPrefixes()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var servers = await db.Guilds.Where(w => true).ToListAsync();
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