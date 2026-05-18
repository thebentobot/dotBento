using CSharpFunctionalExtensions;
using NetCord;
using NetCord.Rest;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Services;
using dotBento.Bot.Utilities;
using dotBento.Infrastructure.Utilities;

namespace dotBento.Bot.Commands.SharedCommands;

public sealed class BannerCommand(StylingUtilities stylingUtilities)
{
    public async Task<ResponseModel> Command(Maybe<User> user)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        if (user.HasNoValue)
        {
            return GenericEmbedService.ErrorEmbed("Error", "That user does not exist.");
        }
        var bannerUrl = user.Value.GetBannerUrl()?.ToString(1024);
        if (bannerUrl == null)
        {
            return GenericEmbedService.ErrorEmbed("Error", "That user does not have a banner.");
        }
        var bannerColour = await stylingUtilities.GetDominantColorAsync(bannerUrl);
        embed.Embed.WithTitle($"{StringUtilities.AddPossessiveS(user.Value.GlobalName ?? user.Value.Username)} User Profile Banner")
            .WithColor(bannerColour)
            .WithImage(new EmbedImageProperties(bannerUrl));
        return embed;
    }
}
