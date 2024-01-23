using Discord;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Utilities;

namespace dotBento.Bot.Commands.SharedCommands;

public class UserCommand(StylingUtilities stylingUtilities)
{
    public async Task<ResponseModel> Command(SocketUser user)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var avatar = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl();
        var avatarForColour = user.GetAvatarUrl(ImageFormat.WebP) ?? user.GetDefaultAvatarUrl();
        var userPfpColour = await stylingUtilities.GetDominantColorAsync(avatarForColour);
        embed.Embed
            .WithColor(userPfpColour)
            .WithTitle($"Profile for {user.GlobalName}")
            .WithThumbnailUrl(avatar)
            .AddField("Username", user.Username)
            .AddField("User ID", user.Id.ToString())
            .AddField("Account created on", $"<t:{user.CreatedAt.ToUnixTimeSeconds()}:F>");
        
        return embed;
    }
}