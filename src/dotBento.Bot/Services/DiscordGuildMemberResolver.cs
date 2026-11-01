using System.Net;
using CSharpFunctionalExtensions;
using Discord;
using Discord.Net;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace dotBento.Bot.Services;

public interface IDiscordGuildMemberResolver
{
    Task<Result<Maybe<IGuildUser>>> ResolveAsync(IGuild guild, ulong userId, IGuildUser? interactionMember = null);
}

/// <summary>Resolves one member without downloading or retaining a server's member list.</summary>
public sealed class DiscordGuildMemberResolver : IDiscordGuildMemberResolver, IDisposable
{
    // Autocomplete resolves a permission check on every keystroke, and a cache miss
    // costs a Discord REST call that can be rate limited or fail transiently. A short
    // lease on the result keeps valid commands available without holding members long.
    internal static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
    internal const int CacheCapacity = 4_096;

    private readonly MemoryCache _resolved = new(new MemoryCacheOptions { SizeLimit = CacheCapacity });

    public async Task<Result<Maybe<IGuildUser>>> ResolveAsync(
        IGuild guild, ulong userId, IGuildUser? interactionMember = null)
    {
        if (interactionMember is { } supplied && supplied.Id == userId && supplied.GuildId == guild.Id)
            return Result.Success(supplied.AsMaybe());

        var key = $"{guild.Id}:{userId}";
        if (_resolved.TryGetValue(key, out Maybe<IGuildUser> known))
            return Result.Success(known);

        try
        {
            var cached = await guild.GetUserAsync(userId, CacheMode.CacheOnly);
            if (cached.AsMaybe().HasValue)
                return Result.Success(Remember(key, cached.AsMaybe()));

            // SocketGuild's IGuild implementation performs a targeted REST call on a cache miss.
            var member = await guild.GetUserAsync(userId, CacheMode.AllowDownload,
                new RequestOptions { Timeout = 10_000 });
            return Result.Success(Remember(key, member.AsMaybe()));
        }
        catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound)
        {
            // Remember the absence too, otherwise a departed member costs a REST call per request.
            return Result.Success(Remember(key, Maybe<IGuildUser>.None));
        }
        catch (Exception ex) when (ex is HttpException or HttpRequestException or TaskCanceledException or TimeoutException)
        {
            // Transport failures are not cached, so the next attempt can succeed.
            Log.Warning(ex, "Could not resolve Discord member {UserId} in guild {GuildId}", userId, guild.Id);
            return Result.Failure<Maybe<IGuildUser>>("Could not retrieve the server member. Please try again later.");
        }
    }

    private Maybe<IGuildUser> Remember(string key, Maybe<IGuildUser> member)
    {
        _resolved.Set(key, member, new MemoryCacheEntryOptions
        {
            Size = 1,
            AbsoluteExpirationRelativeToNow = CacheDuration
        });
        return member;
    }

    public void Dispose() => _resolved.Dispose();
}

public static class GuildMemberPermissions
{
    public static async Task<bool> HasAsync(
        IDiscordGuildMemberResolver resolver, IGuild guild, IUser user, GuildPermission permission)
    {
        var result = await resolver.ResolveAsync(guild, user.Id, user as IGuildUser);
        if (result.IsFailure || result.Value.HasNoValue)
            return false;

        var member = result.Value.Value;
        if (member.GuildId != guild.Id)
            return false;
        if (member.Id == guild.OwnerId)
            return true;

        // REST members need not have a parent guild. Resolve permissions from the known guild's roles.
        var permissions = guild.EveryoneRole.Permissions.RawValue;
        foreach (var roleId in member.RoleIds)
        {
            var role = guild.GetRole(roleId).AsMaybe();
            if (role.HasValue)
                permissions |= role.Value.Permissions.RawValue;
        }

        return (permissions & (ulong)GuildPermission.Administrator) != 0 ||
               (permissions & (ulong)permission) == (ulong)permission;
    }
}
