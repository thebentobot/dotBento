using NetCord;
using NetCord.Gateway;

namespace dotBento.Bot.Extensions;

public static class GuildUserExtensions
{
    /// <summary>
    /// Computes whether a guild user has a given permission, based on their roles within the gateway guild.
    /// </summary>
    public static bool HasGuildPermission(this GuildUser guildUser, Guild guild, Permissions permission)
    {
        if (guild.OwnerId == guildUser.Id)
            return true;

        var accumulated = (Permissions)0;
        foreach (var roleId in guildUser.RoleIds.Concat([guild.Id]))
        {
            if (guild.Roles.TryGetValue(roleId, out var role))
                accumulated |= role.Permissions;
        }

        return accumulated.HasFlag(Permissions.Administrator) || accumulated.HasFlag(permission);
    }
}
