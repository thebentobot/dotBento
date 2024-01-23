using Discord;
using Discord.Commands;
using Discord.Interactions;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;

namespace dotBento.Bot.Commands.SharedCommands;

public static class AboutCommand
{
    public static Task<ResponseModel> Command(SocketCommandContext context)
    {
        var bannerPfp = context.Client.GetUser(232584569289703424).GetDisplayAvatarUrl();
        var botPfp = context.Client.CurrentUser.GetDisplayAvatarUrl();
        var embed = AboutEmbed(botPfp, bannerPfp);

        return Task.FromResult(embed);
    }
    
    public static Task<ResponseModel> Command(SocketInteractionContext context)
    {
        var bannerPfp = context.Client.GetUser(232584569289703424).GetDisplayAvatarUrl();
        var botPfp = context.Client.CurrentUser.GetDisplayAvatarUrl();
        var embed = AboutEmbed(botPfp, bannerPfp);

        return Task.FromResult(embed);
    }

    private static ResponseModel AboutEmbed(string botPfp, string bannerPfp)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed
            .WithColor(DiscordConstants.BentoYellow)
            .WithThumbnailUrl(botPfp)
            .WithTitle("About Bento Bot 🍱")
            .WithDescription("A Discord bot for chat moderation and fun features you did not know you needed on Discord.")
            .AddField("Get a full list and more details for each command", "https://www.bentobot.xyz/commands")
            .AddField("Want additional benefits when using Bento 🍱?", "https://www.patreon.com/bentobot")
            .AddField("Get a Bento 🍱 for each tip", "https://ko-fi.com/bentobot")
            .AddField("Vote on top.gg and receive 5 Bento 🍱", "https://top.gg/bot/787041583580184609/vote")
            .AddField("Want to check out the code for Bento 🍱?", "https://github.com/thebentobot/bento")
            .AddField("Need help? Or do you have some ideas or feedback to Bento 🍱? Feel free to join the support server", "https://discord.gg/dd68WwP")
            .WithFooter(new EmbedFooterBuilder()
            {
                Text = "Bento 🍱 is created by banner.",
                IconUrl = bannerPfp,
            });
        return embed;
    }
}