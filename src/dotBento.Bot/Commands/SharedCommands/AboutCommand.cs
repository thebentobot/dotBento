using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;

namespace dotBento.Bot.Commands.SharedCommands;

public static class AboutCommand
{
    public static Task<ResponseModel> Command(CommandContext context)
    {
        var botPfp = context.Client.Cache.User?.GetAvatarUrl()?.ToString(1024);
        var embed = AboutEmbed(botPfp, null);

        return Task.FromResult(embed);
    }

    public static Task<ResponseModel> Command(ApplicationCommandContext context)
    {
        var botPfp = context.Client.Cache.User?.GetAvatarUrl()?.ToString(1024);
        var embed = AboutEmbed(botPfp, null);

        return Task.FromResult(embed);
    }

    private static ResponseModel AboutEmbed(string? botPfp, string? bannerPfp)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed
            .WithColor(DiscordConstants.BentoYellow)
            .WithThumbnail(botPfp != null ? new EmbedThumbnailProperties(botPfp) : null)
            .WithTitle("About Bento Bot 🍱")
            .WithDescription("A Discord bot for chat moderation and fun features you did not know you needed on Discord.")
            .AddFields([
                new EmbedFieldProperties().WithName("Get a full list and more details for each command").WithValue($"{DiscordConstants.WebsiteUrl}/commands"),
                new EmbedFieldProperties().WithName("Want additional benefits when using Bento 🍱?").WithValue("https://www.patreon.com/bentobot"),
                new EmbedFieldProperties().WithName("Get a Bento 🍱 for each tip").WithValue("https://ko-fi.com/bentobot"),
                new EmbedFieldProperties().WithName("Vote on top.gg and receive 5 Bento 🍱").WithValue("https://top.gg/bot/787041583580184609/vote"),
                new EmbedFieldProperties().WithName("Want to check out the code for Bento 🍱?").WithValue("https://github.com/thebentobot/bento"),
                new EmbedFieldProperties().WithName("Need help? Or do you have some ideas or feedback to Bento 🍱? Feel free to join the support server").WithValue("https://discord.gg/dd68WwP"),
            ])
            .WithFooter(new EmbedFooterProperties()
            {
                Text = "Bento 🍱 is created by banner.",
                IconUrl = bannerPfp,
            });
        return embed;
    }
}
