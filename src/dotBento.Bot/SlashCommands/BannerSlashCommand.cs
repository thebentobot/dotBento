using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using Fergun.Interactive;

namespace dotBento.Bot.SlashCommands;

public class BannerSlashCommand(InteractiveService interactiveService) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("banner", "Get the banner of a User Profile")]
    public async Task UserBannerCommand(
        [Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show banner for you")] bool? hide = false
    )
    {
        user ??= Context.User;
        var restUser = (await Context.Client.Rest.GetUserAsync(user.Id)).AsMaybe();
        if (restUser.HasNoValue)
        {
            await Context.SendResponse(interactiveService, new ResponseModel{ ResponseType = ResponseType.Text, Text = "Sorry, I couldn't find that user." }, true);
        } else
        {
            if (restUser.Value.GetBannerUrl(ImageFormat.Auto, 2048) == null)
            {
                await Context.SendResponse(interactiveService, new ResponseModel{ ResponseType = ResponseType.Text, Text = "Sorry, that user doesn't have a banner." }, true);
                return;
            }
            var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
            embed.Embed.WithTitle($"{user.Username}'s User Profile Banner")
                .WithImageUrl(restUser.Value.GetBannerUrl(size: 2048, format: ImageFormat.Auto));
            await Context.SendResponse(interactiveService, embed, hide ?? false);
        }
    }
}