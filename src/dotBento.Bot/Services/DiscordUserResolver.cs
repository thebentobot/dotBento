using CSharpFunctionalExtensions;
using Microsoft.Extensions.Caching.Memory;
using NetCord;
using NetCord.Gateway;
using Serilog;

namespace dotBento.Bot.Services;

public interface IDiscordUserResolver
{
    /// <summary>
    /// Returns a User, preferring the gateway cache then REST. The gateway-cached User
    /// does NOT include profile banner data — use <see cref="GetRestUserAsync"/> for that.
    /// </summary>
    ValueTask<User?> GetUserAsync(ulong userId);

    /// <summary>
    /// Always fetches the full REST User (which includes banner data), with a 1-hour
    /// in-process cache so repeated lookups don't hammer the API.
    /// </summary>
    Task<Maybe<User>> GetRestUserAsync(ulong userId);
}

public sealed class DiscordUserResolver(GatewayClient client, IMemoryCache cache) : IDiscordUserResolver
{
    private static readonly MemoryCacheEntryOptions RestUserCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    private static string RestUserKey(ulong userId) => $"rest_user:{userId}";

    public async ValueTask<User?> GetUserAsync(ulong userId)
    {
        var cachedUser = client.Cache.Guilds.Values
            .Select(g => g.Users.GetValueOrDefault(userId))
            .FirstOrDefault(u => u is not null);
        if (cachedUser is not null)
            return cachedUser;

        return (await GetRestUserAsync(userId)).GetValueOrDefault();
    }

    public async Task<Maybe<User>> GetRestUserAsync(ulong userId)
    {
        var key = RestUserKey(userId);
        if (cache.TryGetValue(key, out User? cached) && cached is not null)
            return cached;

        try
        {
            var user = await client.Rest.GetUserAsync(userId);
            cache.Set(key, user, RestUserCacheOptions);
            return user;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not fetch REST user {UserId}", userId);
            return Maybe<User>.None;
        }
    }
}
