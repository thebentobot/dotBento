using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Infrastructure.Utilities;

namespace dotBento.Bot.Commands.SharedCommands;

public sealed class ServerCommand(StylingUtilities stylingUtilities)
{
    public async Task<ResponseModel> UserServerCommand(GuildUser guildMember, Guild guild)
    {
        var avatar = guildMember.GetGuildAvatarUrl()?.ToString(1024) ?? guildMember.GetAvatarUrl()?.ToString(1024);
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var guildIconUrl = guild.IconHash != null ? $"https://cdn.discordapp.com/icons/{guild.Id}/{guild.IconHash}.png" : null;
        var guildIconColour = guildIconUrl != null
            ? await stylingUtilities.GetDominantColorAsync(guildIconUrl)
            : DiscordConstants.BentoYellow;
        var displayName = guildMember.Nickname ?? guildMember.GlobalName ?? guildMember.Username;
        var embedAuthor = new EmbedAuthorProperties()
            .WithName(guild.Name)
            .WithIconUrl(guildIconUrl);
        embed.Embed
            .WithAuthor(embedAuthor)
            .WithColor(guildIconColour)
            .WithTitle($"Profile for {displayName}")
            .WithThumbnail(avatar != null ? new EmbedThumbnailProperties(avatar) : null)
            .AddFields([
                new EmbedFieldProperties().WithName("Username").WithValue(guildMember.Username),
                new EmbedFieldProperties().WithName("User ID").WithValue(guildMember.Id.ToString()),
            ]);

        if (guildMember.JoinedAt.HasValue)
            embed.Embed.AddFields([new EmbedFieldProperties().WithName("User joined on").WithValue($"<t:{guildMember.JoinedAt.Value.ToUnixTimeSeconds()}:F>")]);

        embed.Embed.AddFields([new EmbedFieldProperties().WithName("Account created on").WithValue($"<t:{guildMember.CreatedAt.ToUnixTimeSeconds()}:F>")]);

        if (guildMember.GuildBoostStart.HasValue)
            embed.Embed.AddFields([new EmbedFieldProperties().WithName("User boosted on").WithValue($"<t:{guildMember.GuildBoostStart.Value.ToUnixTimeSeconds()}:F>")]);

        var rolesMentions = guildMember.RoleIds
            .Select(roleId => guild.Roles.TryGetValue(roleId, out var role) ? $"<@&{role.Id}>" : $"<@&{roleId}>")
            .ToList();
        embed.Embed.AddFields([new EmbedFieldProperties().WithName("User Roles").WithValue(
            rolesMentions.Count > 0 ? string.Join(", ", rolesMentions) : "None")]);

        return embed;
    }

    public async Task<ResponseModel> ServerInfoCommand(Guild guild)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var guildIconUrl = guild.IconHash != null ? $"https://cdn.discordapp.com/icons/{guild.Id}/{guild.IconHash}.png" : null;
        var serverPfpColour = guildIconUrl != null
            ? await stylingUtilities.GetDominantColorAsync(guildIconUrl)
            : DiscordConstants.BentoYellow;
        embed.Embed
            .WithColor(serverPfpColour)
            .WithTitle($"Server Info for {guild.Name}")
            .WithThumbnail(guildIconUrl != null ? new EmbedThumbnailProperties(guildIconUrl) : null)
            .AddFields([
                new EmbedFieldProperties().WithName("Server ID").WithValue(guild.Id.ToString()),
                new EmbedFieldProperties().WithName("Server created on").WithValue($"<t:{guild.CreatedAt.ToUnixTimeSeconds()}:F>"),
                new EmbedFieldProperties().WithName("Server Owner").WithValue($"<@{guild.OwnerId}>"),
                new EmbedFieldProperties().WithName("Server Members").WithValue($"{guild.UserCount} members"),
                new EmbedFieldProperties().WithName("Server Verification Level").WithValue(guild.VerificationLevel.ToString()),
            ]);

        if (guild.PreferredLocale != null)
            embed.Embed.AddFields([new EmbedFieldProperties().WithName("Server Preferred Locale").WithValue(guild.PreferredLocale)]);

        if (guild.PremiumSubscriptionCount > 0)
            embed.Embed.AddFields([
                new EmbedFieldProperties().WithName("Server Boosts").WithValue(guild.PremiumSubscriptionCount.ToString()),
                new EmbedFieldProperties().WithName("Server Boost Level").WithValue(guild.PremiumTier.ToString()),
            ]);

        var rolesMentions = guild.Roles.Values
            .OrderBy(x => x.Position)
            .Select(x => $"<@&{x.Id}>")
            .ToList();
        embed.Embed.AddFields([new EmbedFieldProperties().WithName("Server Roles").WithValue(
            rolesMentions.Count > 0 ? string.Join(", ", rolesMentions) : "None")]);

        return embed;
    }
}
