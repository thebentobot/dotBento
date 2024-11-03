using CSharpFunctionalExtensions;
using Discord;
using Discord.Rest;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Utilities;

namespace dotBento.Bot.Commands.SharedCommands;

public sealed class BannerCommand(StylingUtilities stylingUtilities)
{
    public async Task<ResponseModel> Command(Maybe<RestUser> user)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        if (user.HasNoValue)
        {
            embed.Embed.WithTitle("Error").WithDescription("That user does not exist.").WithColor(Color.Red);
            return embed;
        }
        if (user.Value.GetBannerUrl(ImageFormat.Auto, 2048) == null)
        {
            embed.Embed.WithTitle("Error").WithDescription("That user does not have a banner.").WithColor(Color.Red);
            return embed;
        }
        var bannerColour = await stylingUtilities.GetDominantColorAsync(user.Value.GetBannerUrl(ImageFormat.WebP, 128));
        embed.Embed.WithTitle($"{user.Value.GlobalName}'s User Profile Banner")
            .WithColor(bannerColour)
            .WithImageUrl(user.Value.GetBannerUrl(size: 2048, format: ImageFormat.Auto));
        return embed;
    }
}