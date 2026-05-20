using CSharpFunctionalExtensions;
using Discord;
using Discord.Rest;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Services;
using dotBento.Bot.Utilities;
using dotBento.Infrastructure.Utilities;

namespace dotBento.Bot.Commands.SharedCommands;

public sealed class BannerCommand(StylingUtilities stylingUtilities)
{
    public async Task<ResponseModel> Command(Maybe<RestUser> user)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        if (user.HasNoValue)
        {
            return GenericEmbedService.ErrorEmbed("Error", "That user does not exist.");
        }
        if (user.Value.GetBannerUrl(ImageFormat.Auto, 2048) == null)
        {
            return GenericEmbedService.ErrorEmbed("Error", "That user does not have a banner.");
        }
        var bannerColour = await stylingUtilities.GetDominantColorAsync(user.Value.GetBannerUrl(ImageFormat.WebP, 128));
        embed.Embed.WithTitle($"{StringUtilities.AddPossessiveS(user.Value.GlobalName)} User Profile Banner")
            .WithColor(bannerColour)
            .WithImageUrl(user.Value.GetBannerUrl(size: 2048, format: ImageFormat.Auto));
        return embed;
    }
}