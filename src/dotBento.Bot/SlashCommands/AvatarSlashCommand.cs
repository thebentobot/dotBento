using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Utilities;
using Fergun.Interactive;

namespace dotBento.Bot.SlashCommands;

[Group("avatar", "Get the avatar of a user")]
public class AvatarSlashCommand(InteractiveService interactiveService, StylingUtilities stylingUtilities) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("user", "Get the avatar of a User Profile")]
    public async Task UserAvatarCommand(
        [Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show avatar for you")] bool? hide = false
        )
    {
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var userPfpColour = await stylingUtilities.GetDominantColorAsync(user.GetAvatarUrl(ImageFormat.WebP, 128));
        embed.Embed.WithTitle($"{user.GlobalName}'s User Profile Avatar")
            .WithColor(userPfpColour)
            .WithImageUrl(user.GetAvatarUrl(size: 2048, format: ImageFormat.Auto));
        await Context.SendResponse(interactiveService, embed, hide ?? false);
    }
    
    [EnabledInDm(false)]
    [SlashCommand("server", "Get the avatar of a Server Profile")]
    public async Task ServerAvatarCommand(
        [Summary("user", "Pick a User")] SocketGuildUser? user = null,
        [Summary("hide", "Only show avatar for you")] bool? hide = false
    )
    {
        user ??= Context.Guild.Users.Single(x => x.Id == Context.User.Id);
        await user.ReturnIfBot(Context, interactiveService);
        var name = user.Nickname ?? user.DisplayName;
        var userPfpColour = await stylingUtilities.GetDominantColorAsync(user.GetGuildAvatarUrl(ImageFormat.WebP, 128));
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle($"{name}'s Server Profile Avatar")
            .WithColor(userPfpColour)
            .WithImageUrl(user.GetGuildAvatarUrl(size: 2048, format: ImageFormat.Auto));
        await Context.SendResponse(interactiveService, embed, hide ?? false);
    }
}