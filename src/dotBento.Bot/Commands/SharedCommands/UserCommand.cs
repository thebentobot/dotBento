using Discord;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services;
using dotBento.Infrastructure.Utilities;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.SharedCommands;

public class UserCommand(
    StylingUtilities stylingUtilities,
    ProfileCommands profileCommands,
    IOptions<BotEnvConfig> config,
    UserService userService)
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
    
    public async Task<ResponseModel> GetProfileAsync(long userId, long guildId, SocketGuildUser guildMember, int guildMemberCount, string botAvatarUrl)
    {
        var result = new ResponseModel
        {
            ResponseType = ResponseType.ImageOnly,
        };
        await userService.CreateOrAddUserToCache(guildMember);
        var imageServerHost = config.Value.ImageServer.ImageServerHost;
        var lastFmApiKey = config.Value.LastFmApiKey;
        var profile =
            await profileCommands.GetProfileAsync(imageServerHost, lastFmApiKey, userId, guildId, guildMember, guildMemberCount, botAvatarUrl);

        if (profile.IsFailure)
        {
            result.ResponseType = ResponseType.Embed;
            result.Embed
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription("Something went wrong while trying to get the profile. Please try again later. Feel free to contact support if the issue persists.");
            return result;
        }
        
        result.Stream = profile.Value;
        result.FileName = $"{guildMember.Username}_BentoProfile_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
        
        return result;
    }
}