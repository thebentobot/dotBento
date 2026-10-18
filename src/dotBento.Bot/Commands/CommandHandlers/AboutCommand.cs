using Discord;
using Discord.Interactions;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Services;
using dotBento.Domain;

namespace dotBento.Bot.Commands.CommandHandlers;

public static class AboutCommand
{
    public static Task<ResponseModel> Command(SocketInteractionContext context, BotPresentation? presentation = null) =>
        Task.FromResult(BuildEmbed(context.Client.CurrentUser.GetDisplayAvatarUrl(), presentation ?? new()));

    internal static ResponseModel BuildEmbed(string botPfp, BotPresentation presentation)
    {
        var brand = presentation.Brand;
        var response = new ResponseModel { ResponseType = ResponseType.Embed };
        response.Embed.WithColor(presentation.Primary)
            .WithThumbnailUrl(botPfp)
            .WithTitle($"About {brand.Name} Bot {brand.Emoji}".Trim())
            .WithDescription(brand.Description);
        BotPresentation.AddLink(response.Embed, $"Explore {brand.Name}'s features", brand.GetFeaturesUrl());
        BotPresentation.AddLink(response.Embed, "Browse all commands and guides", brand.DocumentationUrl);
        if (presentation.Config.Features.PatreonEnabled)
            BotPresentation.AddLink(response.Embed, $"Want additional benefits when using {brand.DisplayName}?", brand.PatreonUrl);
        BotPresentation.AddLink(response.Embed, presentation.DonationLinkTitle, brand.KoFiUrl);
        BotPresentation.AddLink(response.Embed, presentation.VotingLinkTitle, brand.VoteUrl);
        BotPresentation.AddLink(response.Embed, $"Need help or have feedback for {brand.DisplayName}? Join the support server", brand.SupportUrl);
        return response;
    }
}
