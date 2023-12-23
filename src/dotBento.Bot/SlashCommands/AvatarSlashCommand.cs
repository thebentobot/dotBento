using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using Fergun.Interactive;

namespace dotBento.Bot.SlashCommands;

[Group("avatar", "Get the avatar of a user")]
public class AvatarSlashCommand(InteractiveService interactiveService) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("user", "Get the avatar of a User Profile")]
    public async Task UserAvatarCommand(
        [Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show avatar for you")] bool? hide = false
        )
    {
        user ??= Context.User;
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle($"{user.Username}'s User Profile Avatar")
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
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle($"{user.Nickname}'s Server Profile Avatar")
            .WithImageUrl(user.GetGuildAvatarUrl(size: 2048, format: ImageFormat.Auto));
        await Context.SendResponse(interactiveService, embed, hide ?? false);
    }
}