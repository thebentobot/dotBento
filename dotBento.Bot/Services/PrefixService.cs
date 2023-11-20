using System.Collections.Concurrent;
using dotBento.Bot.Interfaces;
using dotBento.Domain;
using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace dotBento.Bot.Services;

public class PrefixService : IPrefixService
{
    private readonly IDbContextFactory<BotDbContext> _contextFactory;

    private static readonly ConcurrentDictionary<ulong, string> ServerPrefixes = new();

    public PrefixService(IDbContextFactory<BotDbContext> contextFactory)
    {
        this._contextFactory = contextFactory;
    }

    public void StorePrefix(string prefix, ulong key)
    {
        if (ServerPrefixes.ContainsKey(key))
        {
            var oldPrefix = GetPrefix(key);
            if (!ServerPrefixes.TryUpdate(key, prefix, oldPrefix))
            {
                Log.Warning($"Failed to update custom prefix {prefix} with the key: {key} from the dictionary");
            }

            return;
        }

        if (!ServerPrefixes.TryAdd(key, prefix))
        {
            Log.Warning($"Failed to add custom prefix {prefix} with the key: {key} from the dictionary");
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
            Log.Warning($"Failed to remove custom prefix {removedPrefix} with the key: {key} from the dictionary");
        }
    }


    public async Task LoadAllPrefixes()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var servers = await db.Guilds.Where(w => w.Prefix != null).ToListAsync();
        foreach (var server in servers)
        {
            StorePrefix(server.Prefix, (ulong)server.GuildId);
        }
    }

    public async Task ReloadPrefix(ulong discordGuildId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
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