using System.Net;
using CSharpFunctionalExtensions;
using Discord;
using Discord.Net;
using Serilog;

namespace dotBento.Bot.Services;

public interface IDiscordGuildMemberResolver
{
    Task<Result<Maybe<IGuildUser>>> ResolveAsync(IGuild guild, ulong userId, IGuildUser? interactionMember = null);
}

/// <summary>Resolves one member without downloading or retaining a server's member list.</summary>
public sealed class DiscordGuildMemberResolver : IDiscordGuildMemberResolver
{
    public async Task<Result<Maybe<IGuildUser>>> ResolveAsync(
        IGuild guild, ulong userId, IGuildUser? interactionMember = null)
    {
        if (interactionMember is { } supplied && supplied.Id == userId && supplied.GuildId == guild.Id)
            return Result.Success(supplied.AsMaybe());

        try
        {
            var cached = await guild.GetUserAsync(userId, CacheMode.CacheOnly);
            if (cached.AsMaybe().HasValue)
                return Result.Success(cached.AsMaybe());

            // SocketGuild's IGuild implementation performs a targeted REST call on a cache miss.
            var member = await guild.GetUserAsync(userId, CacheMode.AllowDownload,
                new RequestOptions { Timeout = 10_000 });
            return Result.Success(member.AsMaybe());
        }
        catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound)
        {
            return Result.Success(Maybe<IGuildUser>.None);
        }
        catch (Exception ex) when (ex is HttpException or HttpRequestException or TaskCanceledException or TimeoutException)
        {
            Log.Warning(ex, "Could not resolve Discord member {UserId} in guild {GuildId}", userId, guild.Id);
            return Result.Failure<Maybe<IGuildUser>>("Could not retrieve the server member. Please try again later.");
        }
    }
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
