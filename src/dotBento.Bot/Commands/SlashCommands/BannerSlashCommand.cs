using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

public class BannerSlashCommand(
    InteractiveService interactiveService,
    BannerCommand bannerCommand) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("banner", "Get the banner of a User Profile")]
    public async Task UserBannerCommand(
        [Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show banner for you")] bool? hide = false
    )
    {
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var restUser = (await Context.Client.Rest.GetUserAsync(user.Id)).AsMaybe();
        var embed = await bannerCommand.Command(restUser);
        var ephemeral = embed.Embed.Color == Color.Red || (hide ?? false);
        await Context.SendResponse(interactiveService, embed, ephemeral);
    }
}