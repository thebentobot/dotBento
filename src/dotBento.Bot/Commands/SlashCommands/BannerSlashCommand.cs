using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

public sealed class BannerSlashCommand(
    InteractiveService interactiveService,
    BannerCommand bannerCommand,
    UserSettingService userSettingService) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("banner", "Get the banner of a User Profile")]
    public async Task UserBannerCommand(
        [Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show banner for you")] bool? hide = null
    )
    {
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var restUser = (await Context.Client.Rest.GetUserAsync(user.Id)).AsMaybe();
        var embed = await bannerCommand.Command(restUser);
        var ephemeral = embed.Embed.Color == Color.Red || (hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        await Context.SendResponse(interactiveService, embed, ephemeral);
    }
}
