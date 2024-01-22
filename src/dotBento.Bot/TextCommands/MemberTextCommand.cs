using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Utilities;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.TextCommands
{
    [Name("Member")]
    public class MemberTextCommand(
        IOptions<BotEnvConfig> botSettings,
        InteractiveService interactiveService, StylingUtilities stylingUtilities) : BaseCommandModule(botSettings)
    {

        [Command("member", RunMode = RunMode.Async)]
        [Summary("Show info for a server user")]
        [Alias("guildMember")]
        [GuildOnly]
        public async Task MemberCommand([Summary("The user to get the Server User info from")] SocketUser user = null)
        {
            _ = Context.Channel.TriggerTypingAsync();
            user ??= Context.User;
            await user.ReturnIfBot(Context, interactiveService);
            var guildMember = Context.Guild.Users.Single(guildUser => guildUser.Id == user.Id);
            var avatar = guildMember.GetGuildAvatarUrl() ?? guildMember.GetDisplayAvatarUrl();
            var userPfpColour = await stylingUtilities.GetDominantColorAsync(
                guildMember.GetGuildAvatarUrl(ImageFormat.WebP, 128) ??
                guildMember.GetDisplayAvatarUrl(ImageFormat.WebP, 128));
            
            var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
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

            var userRoles = guildMember.Roles
                .Where(x => x.Name != "@everyone")
                .OrderBy(x => x.Position)
                .Select(x => x.Mention).ToList();
            embed.Embed.AddField("User Roles", string.Join(", ", userRoles));        
        
            await Context.SendResponse(interactiveService, embed);
        }
    }
}