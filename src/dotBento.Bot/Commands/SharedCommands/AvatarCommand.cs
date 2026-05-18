using NetCord;
using NetCord.Rest;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Utilities;
using dotBento.Infrastructure.Utilities;

namespace dotBento.Bot.Commands.SharedCommands;

public sealed class AvatarCommand(StylingUtilities stylingUtilities)
{
    public async Task<ResponseModel> UserAvatarCommand(User user)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var avatarUrl = user.GetAvatarUrl()?.ToString(1024) ?? user.DefaultAvatarUrl.ToString(1024);
        var userPfpColour = await stylingUtilities.GetDominantColorAsync(avatarUrl);
        embed.Embed.WithTitle($"{StringUtilities.AddPossessiveS(user.GlobalName ?? user.Username)} User Profile Avatar")
            .WithColor(userPfpColour)
            .WithImage(new EmbedImageProperties(avatarUrl));
        return embed;
    }

    public async Task<ResponseModel> ServerAvatarCommand(GuildUser user)
    {
        var name = user.Nickname ?? user.GlobalName ?? user.Username;
        var avatarForColour = user.GetGuildAvatarUrl()?.ToString(1024) ?? user.GetAvatarUrl()?.ToString(1024) ?? user.DefaultAvatarUrl.ToString(1024);
        var avatarForImage = user.GetGuildAvatarUrl()?.ToString(1024) ?? user.GetAvatarUrl()?.ToString(1024) ?? user.DefaultAvatarUrl.ToString(1024);
        var userPfpColour = await stylingUtilities.GetDominantColorAsync(avatarForColour);
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle($"{StringUtilities.AddPossessiveS(name)} Server Profile Avatar")
            .WithColor(userPfpColour)
            .WithImage(new EmbedImageProperties(avatarForImage));
        return embed;
    }
}
