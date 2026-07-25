using Discord;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Bot.Utilities;
using dotBento.Infrastructure.Utilities;
using Serilog;

namespace dotBento.Bot.Commands.SharedCommands;

public sealed class AvatarCommand(StylingUtilities stylingUtilities, Serilog.ILogger? logger = null)
{
    private readonly Serilog.ILogger _logger = logger ?? Log.Logger;

    public async Task<ResponseModel> UserAvatarCommand(IUser user)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var avatarForColour = user.GetAvatarUrl(ImageFormat.WebP) ?? user.GetDefaultAvatarUrl();
        var avatarForImage = user.GetAvatarUrl(size: 2048, format: ImageFormat.Auto) ?? user.GetDefaultAvatarUrl();
        var userPfpColour = await GetAvatarColourAsync(avatarForColour);
        var name = user.GlobalName ?? user.Username;
        embed.Embed.WithTitle($"{StringUtilities.AddPossessiveS(name)} User Profile Avatar")
            .WithColor(userPfpColour)
            .WithImageUrl(avatarForImage);
        return embed;
    }
    
    public async Task<ResponseModel> ServerAvatarCommand(IGuildUser user)
    {
        var name = user.Nickname ?? user.DisplayName;
        var avatarForColour = user.GetGuildAvatarUrl(ImageFormat.WebP) ?? user.GetDisplayAvatarUrl(ImageFormat.WebP);
        var avatarForImage = user.GetGuildAvatarUrl(size: 2048, format: ImageFormat.Auto) ?? user.GetDisplayAvatarUrl(size: 2048, format: ImageFormat.Auto);
        var userPfpColour = await GetAvatarColourAsync(avatarForColour);
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle($"{StringUtilities.AddPossessiveS(name)} Server Profile Avatar")
            .WithColor(userPfpColour)
            .WithImageUrl(avatarForImage);
        return embed;
    }

    private async Task<Color> GetAvatarColourAsync(string avatarUrl)
    {
        var result = await stylingUtilities.TryGetDominantColorAsync(avatarUrl);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        _logger.Warning("Could not calculate avatar colour; using fallback colour. {Error}", result.Error);
        return DiscordConstants.BentoYellow;
    }
}
