using Discord;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Utilities;

namespace dotBento.Bot.Commands.SharedCommands;

public sealed class AvatarCommand(StylingUtilities stylingUtilities)
{
    public async Task<ResponseModel> UserAvatarCommand(SocketUser user)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var userPfpColour = await stylingUtilities.GetDominantColorAsync(user.GetAvatarUrl(ImageFormat.WebP));
        embed.Embed.WithTitle($"{user.GlobalName}'s User Profile Avatar")
            .WithColor(userPfpColour)
            .WithImageUrl(user.GetAvatarUrl(size: 2048, format: ImageFormat.Auto));
        return embed;
    }
    
    public async Task<ResponseModel> ServerAvatarCommand(SocketGuildUser user)
    {
        var name = user.Nickname ?? user.DisplayName;
        var avatarForColour = user.GetGuildAvatarUrl(ImageFormat.WebP) ?? user.GetDisplayAvatarUrl(ImageFormat.WebP);
        var avatarForImage = user.GetGuildAvatarUrl(size: 2048, format: ImageFormat.Auto) ?? user.GetDisplayAvatarUrl(size: 2048, format: ImageFormat.Auto);
        var userPfpColour = await stylingUtilities.GetDominantColorAsync(avatarForColour);
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle($"{name}'s Server Profile Avatar")
            .WithColor(userPfpColour)
            .WithImageUrl(avatarForImage);
        return embed;
    }
}