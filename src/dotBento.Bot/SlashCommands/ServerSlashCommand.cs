using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.EntityFramework.Context;
using dotBento.Infrastructure.Utilities;
using Fergun.Interactive;

namespace dotBento.Bot.SlashCommands;

[Group("server", "Commands for the Discord Server")]
public class ServerSlashCommand(BotDbContext botDbContext, InteractiveService interactiveService, StylingUtilities stylingUtilities)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("user", "Show info for a server user")]
    [EnabledInDm(false)]
    public async Task UserServerCommand([Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show server user info for you")] bool? hide = false)
    {
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var guildMember = Context.Guild.Users.Single(guildUser => guildUser.Id == user.Id);
        var avatar = guildMember.GetGuildAvatarUrl() ?? guildMember.GetDisplayAvatarUrl();
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var userPfpColour = await stylingUtilities.GetDominantColorAsync(guildMember.GetGuildAvatarUrl(ImageFormat.WebP, 128) ?? guildMember.GetDisplayAvatarUrl(ImageFormat.WebP, 128));
        embed.Embed
            .WithColor(userPfpColour)
            .WithTitle($"Profile for {guildMember.Nickname ?? guildMember.DisplayName}")
            .WithThumbnailUrl(avatar)
            .AddField("Username", guildMember.Username)
            .AddField("User ID", guildMember.Id.ToString());
        
        if (guildMember.JoinedAt.HasValue)
        {
            embed.Embed.AddField("User joined on", $"<t:{guildMember.JoinedAt.Value.ToUnixTimeSeconds()}:F>");
        }
        
        embed.Embed.AddField("Account created on", $"<t:{user.CreatedAt.ToUnixTimeSeconds()}:F>");
        
        if (guildMember.PremiumSince.HasValue)
        {
            embed.Embed.AddField("User boosted on", $"<t:{guildMember.PremiumSince.Value.ToUnixTimeSeconds()}:F>");
        }

        embed.Embed.AddField("User Roles", string.Join(", ", guildMember.Roles.OrderBy(x => x.Position).Select(x => x.Mention).ToList()));        
        
        await Context.SendResponse(interactiveService, embed, hide ?? false);
    }
    
    [SlashCommand("info", "Show info for the server")]
    [EnabledInDm(false)]
    public async Task ServerCommand([Summary("hide", "Only show server info for you")] bool? hide = false)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var serverPfpColour = await stylingUtilities.GetDominantColorAsync(Context.Guild.IconUrl);
        embed.Embed
            .WithColor(serverPfpColour)
            .WithTitle($"Server Info for {Context.Guild.Name}")
            .WithThumbnailUrl(Context.Guild.IconUrl)
            .AddField("Server ID", Context.Guild.Id.ToString())
            .AddField("Server created on", $"<t:{Context.Guild.CreatedAt.ToUnixTimeSeconds()}:F>")
            .AddField("Server Owner", Context.Guild.Owner.Mention)
            .AddField("Server Members", $"{Context.Guild.MemberCount} members")
            .AddField("Server Verification Level", Context.Guild.VerificationLevel.ToString());
        
        if (Context.Guild.PreferredLocale != null)
        {
            embed.Embed.AddField("Server Preferred Locale", Context.Guild.PreferredLocale);
        }
        
        if (Context.Guild.PremiumSubscriptionCount > 0)
        {
            embed.Embed
                .AddField("Server Boosts", Context.Guild.PremiumSubscriptionCount.ToString())
                .AddField("Server Boost Level", Context.Guild.PremiumTier.ToString());
        }

        embed.Embed.AddField("Server Roles", string.Join(", ", Context.Guild.Roles.OrderBy(x => x.Position).Select(x => x.Mention).ToList()));        
        await Context.SendResponse(interactiveService, embed, hide ?? false);
    }
}