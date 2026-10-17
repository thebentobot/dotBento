using Discord;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;

namespace dotBento.Bot.Services;

public static class GenericEmbedService
{
    public static ResponseModel ErrorEmbed(string title, string? description = null)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Embed };
        embed.Embed.WithColor(DiscordConstants.ErrorRed).WithTitle(title);
        if (description is not null)
            embed.Embed.WithDescription(description);
        return embed;
    }
}
