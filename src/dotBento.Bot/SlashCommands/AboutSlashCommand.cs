using Discord;
using Discord.Interactions;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using Fergun.Interactive;

namespace dotBento.Bot.SlashCommands;

[Group("about", "Information about stuff related to Bento")]
public class AboutSlashCommand(InteractiveService interactiveService)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("bento", "Show info about Bento")]
    public async Task UserCommand(
        [Summary("hide", "Only show info for you")] bool? hide = false)
    {
        var bannerPfp = Context.Client.GetUser(232584569289703424).GetDefaultAvatarUrl();
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed
            .WithColor(DiscordConstants.BentoYellow)
            .WithThumbnailUrl(Context.Client.CurrentUser.GetDefaultAvatarUrl())
            .WithTitle("About Bento Bot 🍱")
            .WithDescription("A Discord bot for chat moderation and fun features you did not know you needed on Discord.")
            .AddField("About Bento Bot 🍱", "A Discord bot for chat moderation and fun features you did not know you needed on Discord.")
            .AddField("Get a full list and more details for each command", "https://www.bentobot.xyz/commands")
            .AddField("Want additional benefits when using Bento 🍱?", "https://www.patreon.com/bentobot")
            .AddField("Get a Bento 🍱 for each tip", "https://ko-fi.com/bentobot")
            .AddField("Vote on top.gg and receive 5 Bento 🍱", "https://top.gg/bot/787041583580184609/vote")
            .AddField("Want to check out the code for Bento 🍱?", "https://github.com/thebentobot/bento")
            .AddField("Need help? Or do you have some ideas or feedback to Bento 🍱? Feel free to join the support server", "https://discord.gg/dd68WwP")
            .WithFooter(new EmbedFooterBuilder()
            {
                Text = "Bento 🍱 is created by `banner.`",
                IconUrl = bannerPfp,
            });
        
        await Context.SendResponse(interactiveService, embed, hide ?? false);
    }
}