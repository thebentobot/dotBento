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
    [Name("ServerInfo")]
    public class ServerInfoTextCommand(
        IOptions<BotEnvConfig> botSettings,
        InteractiveService interactiveService, StylingUtilities stylingUtilities) : BaseCommandModule(botSettings)
    {

        [Command("serverInfo", RunMode = RunMode.Async)]
        [Summary("Show info for a server")]
        [Alias("guildInfo")]
        [GuildOnly]
        public async Task ServerInfoCommand()
        {
            _ = Context.Channel.TriggerTypingAsync();
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
            await Context.SendResponse(interactiveService, embed);
        }
    }
}