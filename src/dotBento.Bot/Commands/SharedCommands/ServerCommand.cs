using Discord;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Utilities;

namespace dotBento.Bot.Commands.SharedCommands;

public class ServerCommand(StylingUtilities stylingUtilities)
{
    public async Task<ResponseModel> UserServerCommand(SocketGuildUser guildMember)
    {
        var avatar = guildMember.GetGuildAvatarUrl() ?? guildMember.GetDisplayAvatarUrl();
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var guildIconColour = await stylingUtilities.GetDominantColorAsync(guildMember.Guild.IconUrl);
        var embedAuthor = new EmbedAuthorBuilder()
            .WithName(guildMember.Guild.Name)
            .WithIconUrl(guildMember.Guild.IconUrl);
        embed.Embed
            .WithAuthor(embedAuthor)
            .WithColor(guildIconColour)
            .WithTitle($"Profile for {guildMember.Nickname ?? guildMember.DisplayName}")
            .WithThumbnailUrl(avatar)
            .AddField("Username", guildMember.Username)
            .AddField("User ID", guildMember.Id.ToString());
        
        if (guildMember.JoinedAt.HasValue)
        {
            embed.Embed.AddField("User joined on", $"<t:{guildMember.JoinedAt.Value.ToUnixTimeSeconds()}:F>");
        }
        
        embed.Embed.AddField("Account created on", $"<t:{guildMember.CreatedAt.ToUnixTimeSeconds()}:F>");
        
        if (guildMember.PremiumSince.HasValue)
        {
            embed.Embed.AddField("User boosted on", $"<t:{guildMember.PremiumSince.Value.ToUnixTimeSeconds()}:F>");
        }

        embed.Embed.AddField("User Roles",
            string.Join(", ",
                guildMember.Roles.OrderBy(x => x.Position)
                    .Select(x => x.Mention)
                    .ToList()));        
        
        return embed;
    }

    public async Task<ResponseModel> ServerInfoCommand(SocketGuild guild)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var serverPfpColour = await stylingUtilities.GetDominantColorAsync(guild.IconUrl);
        embed.Embed
            .WithColor(serverPfpColour)
            .WithTitle($"Server Info for {guild.Name}")
            .WithThumbnailUrl(guild.IconUrl)
            .AddField("Server ID", guild.Id.ToString())
            .AddField("Server created on", $"<t:{guild.CreatedAt.ToUnixTimeSeconds()}:F>")
            .AddField("Server Owner", guild.Owner.Mention)
            .AddField("Server Members", $"{guild.MemberCount} members")
            .AddField("Server Verification Level", guild.VerificationLevel.ToString());
        
        if (guild.PreferredLocale != null)
        {
            embed.Embed.AddField("Server Preferred Locale", guild.PreferredLocale);
        }
        
        if (guild.PremiumSubscriptionCount > 0)
        {
            embed.Embed
                .AddField("Server Boosts", guild.PremiumSubscriptionCount.ToString())
                .AddField("Server Boost Level", guild.PremiumTier.ToString());
        }

        embed.Embed.AddField("Server Roles", 
            string.Join(", ", 
                guild.Roles
                    .OrderBy(x => x.Position)
                    .Select(x => x.Mention).ToList()
            )
        );        
        return embed;
    }
}