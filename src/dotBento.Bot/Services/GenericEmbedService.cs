using Discord;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;

namespace dotBento.Bot.Services;

public static class GenericEmbedService
{
    public static ResponseModel ErrorEmbed(string title, string? description = null, BotPresentation? presentation = null)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Embed, CommandResponse = dotBento.Domain.Enums.CommandResponse.Error };
        embed.Embed.WithColor((presentation ?? new()).Error).WithTitle(title);
        if (description is not null)
            embed.Embed.WithDescription(description);
        return embed;
    }
}
