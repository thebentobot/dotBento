using Microsoft.Extensions.Caching.Memory;
using NetCord;
using NetCord.Gateway;
using Serilog;

namespace dotBento.Bot.Services;

/// <summary>
/// Resolves guild members by checking the gateway cache first, then falling back to
/// a short-lived in-process cache and REST. This avoids requiring the privileged
/// GuildMembers intent while still avoiding redundant REST calls.
/// </summary>
public sealed class GuildMemberLookupService(GatewayClient client, IMemoryCache cache)
{
    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    private static string Key(ulong guildId, ulong userId) => $"gm:{guildId}:{userId}";

    /// <summary>
    /// Returns the guild member for <paramref name="userId"/> in <paramref name="guildId"/>.
    /// Checks the gateway guild's Users dictionary first, then an in-process cache, then REST.
    /// Returns null if the user is not a member of the guild.
    /// </summary>
    public async Task<GuildUser?> GetOrFetchAsync(ulong guildId, ulong userId, Guild? gatewayGuild = null)
    {
        var fromGateway = gatewayGuild?.Users.GetValueOrDefault(userId);
        if (fromGateway is not null)
            return fromGateway;

        var key = Key(guildId, userId);
        if (cache.TryGetValue(key, out GuildUser? cached))
            return cached;

        try
        {
            var member = await client.Rest.GetGuildUserAsync(guildId, userId);
            cache.Set(key, member, CacheOptions);
            return member;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not fetch guild member {UserId} in guild {GuildId} via REST", userId, guildId);
            return null;
        }
    }

    /// <summary>Writes an updated member into the cache, replacing any stale entry.</summary>
    public void Update(GuildUser member) =>
        cache.Set(Key(member.GuildId, member.Id), member, CacheOptions);

    /// <summary>Evicts a cached member entry, e.g. after a leave or ban event.</summary>
    public void Invalidate(ulong guildId, ulong userId) =>
        cache.Remove(Key(guildId, userId));
}
